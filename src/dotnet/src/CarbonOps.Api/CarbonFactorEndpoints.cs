using System.Globalization;
using CarbonOps.Application.Factors;
using CarbonOps.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace CarbonOps.Api;

internal static class CarbonFactorEndpoints
{
    private const string ImportEndpointRequiredScope = "carbon_factors:import";
    private const string ImportEndpointPath = "/carbon-factors/import";
    private const string ImportAuthenticationScheme = "api_key";

    private static readonly string[] SupportedSearchFilters =
    [
        "category",
        "activity",
        "region",
        "year",
        "offset",
        "limit"
    ];

    private static readonly string[] SupportedListFilters =
    [
        "offset",
        "limit"
    ];

    public static IEndpointRouteBuilder MapCarbonFactorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapCarbonFactorEndpointGroup(endpoints.MapGroup("/carbon-factors"));
        MapCarbonFactorEndpointGroup(endpoints.MapGroup($"/{CarbonOpsApiVersions.V1}/carbon-factors"));

        return endpoints;
    }

    private static void MapCarbonFactorEndpointGroup(RouteGroupBuilder group)
    {
        group.MapGet("/", (HttpRequest request, CarbonFactorUseCases useCases) =>
            ListCarbonFactors(request, useCases))
            .WithMetadata(CarbonFactorEndpointExamples.ListFactorsSuccess)
            .RequireRateLimiting(CarbonOpsRateLimitingPolicyNames.Read);

        group.MapGet("/search", (HttpRequest request, CarbonFactorUseCases useCases) =>
            SearchCarbonFactors(request, useCases))
            .WithMetadata(
                CarbonFactorEndpointExamples.SearchFactorsSuccess,
                CarbonFactorEndpointExamples.SearchFactorsInvalidQuery)
            .RequireRateLimiting(CarbonOpsRateLimitingPolicyNames.Read);

        group.MapGet("/{factorId}", (string factorId, CarbonFactorUseCases useCases) =>
            GetCarbonFactorById(factorId, useCases))
            .WithMetadata(
                CarbonFactorEndpointExamples.GetFactorByIdSuccess,
                CarbonFactorEndpointExamples.GetFactorByIdNotFound)
            .RequireRateLimiting(CarbonOpsRateLimitingPolicyNames.Read);

        group.MapPost(
            "/import",
            (HttpRequest httpRequest,
                ParserCarbonFactorBatchImportRequest request,
                CarbonFactorImportBoundaryService boundaryService,
                IOptions<ApiKeyAuthenticationOptions> apiKeyOptions,
                ILoggerFactory loggerFactory,
                IAuditEventSink auditEventSink) =>
                ImportCarbonFactors(
                    httpRequest,
                    request,
                    boundaryService,
                    apiKeyOptions.Value,
                    loggerFactory.CreateLogger("CarbonOps.Api.Import"),
                    auditEventSink))
            .RequireRateLimiting(CarbonOpsRateLimitingPolicyNames.Import);
    }

    private static IResult ListCarbonFactors(HttpRequest request, CarbonFactorUseCases useCases)
    {
        var pagination = TryBuildPaginationQuery(request.Query, SupportedListFilters).GetValueOrThrow();

        return TypedResults.Ok(
            useCases
                .ListCarbonFactors(pagination.Query, pagination.RequestedFilterNames)
                .GetValueOrThrow());
    }

    private static IResult GetCarbonFactorById(string factorId, CarbonFactorUseCases useCases)
    {
        ValidateFactorId(factorId).ThrowIfError();

        return TypedResults.Ok(useCases.GetCarbonFactorById(factorId).GetValueOrThrow());
    }

    private static IResult SearchCarbonFactors(HttpRequest request, CarbonFactorUseCases useCases)
    {
        var queryRequest = TryBuildFactorQuery(request.Query).GetValueOrThrow();

        return TypedResults.Ok(
            useCases
                .SearchCarbonFactors(
                    queryRequest.Query,
                    queryRequest.Pagination,
                    queryRequest.RequestedFilterNames)
                .GetValueOrThrow());
    }

    private static async Task<IResult> ImportCarbonFactors(
        HttpRequest httpRequest,
        ParserCarbonFactorBatchImportRequest request,
        CarbonFactorImportBoundaryService boundaryService,
        ApiKeyAuthenticationOptions apiKeyOptions,
        ILogger logger,
        IAuditEventSink auditEventSink)
    {
        var importEndpointPath = ResolveImportEndpointPath(httpRequest.HttpContext);
        var importAuthResult = EnsureAuthorized(httpRequest, apiKeyOptions);
        if (!importAuthResult.IsSuccess)
        {
            LogImportAuthorizationFailure(logger, importEndpointPath, importAuthResult.Error!);
            await auditEventSink.WriteAsync(
                CreateImportAuthorizationFailedAuditEvent(
                    httpRequest.HttpContext,
                    importEndpointPath,
                    ResolveAuthorizationFailureReason(importAuthResult.Error!)),
                httpRequest.HttpContext.RequestAborted);
        }

        var importAuthContext = importAuthResult.GetValueOrThrow();

        var result = boundaryService.ValidateAndAccept(request);
        if (!result.IsSuccess)
        {
            LogImportValidationFailure(logger, importEndpointPath, result.Error!);
            await auditEventSink.WriteAsync(
                CreateImportValidationFailedAuditEvent(
                    httpRequest.HttpContext,
                    importEndpointPath,
                    importAuthContext,
                    result.Error!),
                httpRequest.HttpContext.RequestAborted);
        }

        var acceptedResult = result.GetValueOrThrow();
        var scopedResult = acceptedResult with
        {
            Audit = acceptedResult.Audit with
            {
                TenantId = importAuthContext.TenantId,
                AuthenticationScheme = importAuthContext.AuthenticationScheme
            }
        };

        LogImportAccepted(logger, importEndpointPath, scopedResult);
        await auditEventSink.WriteAsync(
            CreateImportAcceptedAuditEvent(httpRequest.HttpContext, importEndpointPath, scopedResult),
            httpRequest.HttpContext.RequestAborted);

        return TypedResults.Accepted($"{importEndpointPath}/{scopedResult.BatchId}", scopedResult);
    }

    private static ApplicationResult<ImportAuthenticationContext> EnsureAuthorized(HttpRequest request, ApiKeyAuthenticationOptions options)
    {
        var configuredKeyHash = options.ImportEndpointKeyHash?.Trim();
        if (string.IsNullOrWhiteSpace(configuredKeyHash))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("import endpoint API key hash is not configured"));
        }

        if (!ApiKeyHashVerifier.IsValidSha256HexHash(configuredKeyHash))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("import endpoint API key hash is invalid"));
        }

        var previousKeyHashesResult = TryNormalizeConfiguredKeyHashes(
            options.ImportEndpointPreviousKeyHashes,
            "import endpoint previous API key hash is invalid");
        if (!previousKeyHashesResult.IsSuccess)
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(previousKeyHashesResult.Error!);
        }

        var revokedKeyHashesResult = TryNormalizeConfiguredKeyHashes(
            options.RevokedKeyHashes,
            "revoked API key hash is invalid");
        if (!revokedKeyHashesResult.IsSuccess)
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(revokedKeyHashesResult.Error!);
        }

        if (!request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var providedApiKey)
            || string.IsNullOrWhiteSpace(providedApiKey))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("missing API key"));
        }

        var providedKeyHash = ApiKeyHashVerifier.ComputeSha256Hex(providedApiKey.ToString());
        var revokedKeyHashes = revokedKeyHashesResult.Value!;
        if (ApiKeyHashVerifier.MatchesAnySha256HexHash(providedKeyHash, revokedKeyHashes))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("API key is revoked"));
        }

        var acceptedKeyHashes = new[] { configuredKeyHash! }
            .Concat(previousKeyHashesResult.Value!)
            .ToArray();
        if (!ApiKeyHashVerifier.MatchesAnySha256HexHash(providedKeyHash, acceptedKeyHashes))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("invalid API key"));
        }

        if (string.IsNullOrWhiteSpace(options.ImportTenantId))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("import tenant is not configured"));
        }

        var configuredScopeValues = options.ImportEndpointScopes ?? [];
        if (configuredScopeValues.Length == 0
            || configuredScopeValues.Any(scope => string.IsNullOrWhiteSpace(scope)))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("import endpoint scope is not configured"));
        }

        var configuredScopes = configuredScopeValues
            .Select(scope => scope.Trim())
            .ToArray();

        if (!configuredScopes.Contains(ImportEndpointRequiredScope, StringComparer.Ordinal))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("API key is not permitted to import carbon factors"));
        }

        return ApplicationResult<ImportAuthenticationContext>.Success(
            new ImportAuthenticationContext(options.ImportTenantId.Trim(), ImportAuthenticationScheme));
    }

    private static void LogImportAuthorizationFailure(ILogger logger, string endpointPath, ApiError error)
    {
        logger.LogWarning(
            "CarbonOps import authorization failed {endpoint} {auth_failure_reason} {authentication_scheme}",
            endpointPath,
            ResolveAuthorizationFailureReason(error),
            ImportAuthenticationScheme);
    }

    private static void LogImportValidationFailure(ILogger logger, string endpointPath, ApiError error)
    {
        logger.LogInformation(
            "CarbonOps import validation failed {endpoint} {validation_failure_reason}",
            endpointPath,
            ResolveErrorReason(error));
    }

    private static void LogImportAccepted(
        ILogger logger,
        string endpointPath,
        CarbonFactorImportBoundaryResponse response)
    {
        logger.LogInformation(
            "CarbonOps import request accepted {endpoint} {authentication_scheme} {tenant_id} {batch_id} {validation_status} {accepted_records} {rejected_records} {error_count} {warning_count} {persisted} {import_execution}",
            endpointPath,
            response.Audit.AuthenticationScheme,
            response.Audit.TenantId,
            response.BatchId,
            response.ValidationStatus,
            response.AcceptedRecords,
            response.RejectedRecords,
            response.ErrorCount,
            response.WarningCount,
            response.Persisted,
            response.ImportExecution);
    }

    private static AuditEvent CreateImportAuthorizationFailedAuditEvent(
        HttpContext httpContext,
        string endpointPath,
        string reasonCode)
    {
        return CreateAuditEvent(
            eventType: AuditEventTypes.ImportAuthorizationFailed,
            severity: AuditEventSeverity.Warning,
            httpContext: httpContext,
            endpointPath: endpointPath,
            outcome: AuditEventOutcomes.Failure,
            reasonCode: reasonCode,
            authenticationScheme: ImportAuthenticationScheme);
    }

    private static AuditEvent CreateImportValidationFailedAuditEvent(
        HttpContext httpContext,
        string endpointPath,
        ImportAuthenticationContext importAuthContext,
        ApiError error)
    {
        return CreateAuditEvent(
            eventType: AuditEventTypes.ImportValidationFailed,
            severity: AuditEventSeverity.Information,
            httpContext: httpContext,
            endpointPath: endpointPath,
            outcome: AuditEventOutcomes.Failure,
            reasonCode: ResolveValidationFailureReason(error),
            authenticationScheme: importAuthContext.AuthenticationScheme,
            tenantId: importAuthContext.TenantId);
    }

    private static AuditEvent CreateImportAcceptedAuditEvent(
        HttpContext httpContext,
        string endpointPath,
        CarbonFactorImportBoundaryResponse response)
    {
        return CreateAuditEvent(
            eventType: AuditEventTypes.ImportAccepted,
            severity: AuditEventSeverity.Information,
            httpContext: httpContext,
            endpointPath: endpointPath,
            outcome: AuditEventOutcomes.Success,
            authenticationScheme: response.Audit.AuthenticationScheme,
            tenantId: response.Audit.TenantId,
            batchId: response.BatchId,
            validationStatus: response.ValidationStatus,
            acceptedRecords: response.AcceptedRecords,
            rejectedRecords: response.RejectedRecords,
            errorCount: response.ErrorCount,
            warningCount: response.WarningCount,
            persisted: response.Persisted,
            importExecution: response.ImportExecution);
    }

    private static AuditEvent CreateAuditEvent(
        string eventType,
        string severity,
        HttpContext httpContext,
        string endpointPath,
        string outcome,
        string? reasonCode = null,
        string? authenticationScheme = null,
        string? tenantId = null,
        string? batchId = null,
        string? validationStatus = null,
        int? acceptedRecords = null,
        int? rejectedRecords = null,
        int? errorCount = null,
        int? warningCount = null,
        bool? persisted = null,
        string? importExecution = null)
    {
        return new AuditEvent(
            EventId: Guid.NewGuid().ToString(),
            EventType: eventType,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Severity: severity,
            Endpoint: endpointPath,
            CorrelationId: httpContext.GetCorrelationId(),
            Outcome: outcome,
            ReasonCode: reasonCode,
            AuthenticationScheme: authenticationScheme,
            TenantId: tenantId,
            BatchId: batchId,
            ValidationStatus: validationStatus,
            AcceptedRecords: acceptedRecords,
            RejectedRecords: rejectedRecords,
            ErrorCount: errorCount,
            WarningCount: warningCount,
            Persisted: persisted,
            ImportExecution: importExecution);
    }

    private static string ResolveImportEndpointPath(HttpContext httpContext)
    {
        return httpContext.Request.Path.HasValue
            ? httpContext.Request.Path.Value!
            : ImportEndpointPath;
    }

    private static string ResolveAuthorizationFailureReason(ApiError error)
    {
        return ResolveErrorReason(error) switch
        {
            "import endpoint API key hash is not configured" => "missing_current_hash_config",
            "import endpoint API key hash is invalid" => "invalid_current_hash_config",
            "import endpoint previous API key hash is invalid" => "invalid_previous_hash_config",
            "revoked API key hash is invalid" => "invalid_revoked_hash_config",
            "missing API key" => "missing_api_key",
            "API key is revoked" => "revoked_api_key",
            "invalid API key" => "invalid_api_key",
            "import tenant is not configured" => "missing_tenant_config",
            "import endpoint scope is not configured" => "missing_scope_config",
            "API key is not permitted to import carbon factors" => "insufficient_scope",
            _ => "unauthorized"
        };
    }

    private static string ResolveValidationFailureReason(ApiError error)
    {
        return error.Code;
    }

    private static string ResolveErrorReason(ApiError error)
    {
        if (error.Details.TryGetValue("reason", out var reason)
            && reason is string reasonValue
            && !string.IsNullOrWhiteSpace(reasonValue))
        {
            return reasonValue;
        }

        return error.Code;
    }

    private static ApplicationResult<string[]> TryNormalizeConfiguredKeyHashes(
        IEnumerable<string?>? configuredHashes,
        string invalidReason)
    {
        var normalizedHashes = new List<string>();
        foreach (var configuredHash in configuredHashes ?? [])
        {
            var normalizedHash = configuredHash?.Trim();
            if (!ApiKeyHashVerifier.IsValidSha256HexHash(normalizedHash))
            {
                return ApplicationResult<string[]>.Failure(ApiError.Unauthorized(invalidReason));
            }

            normalizedHashes.Add(normalizedHash!);
        }

        return ApplicationResult<string[]>.Success(normalizedHashes.ToArray());
    }

    private static ApplicationResult<FactorSearchRequest> TryBuildFactorQuery(IQueryCollection queryCollection)
    {
        var unsupportedFilters = queryCollection.Keys
            .Where(key => !SupportedSearchFilters.Contains(key, StringComparer.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        if (unsupportedFilters.Length > 0)
        {
            return ApplicationResult<FactorSearchRequest>.Failure(
                ApiError.InvalidQuery($"unsupported filters: {string.Join(", ", unsupportedFilters)}"));
        }

        if (!TryReadOptionalString(queryCollection, "category", out var category, out var categoryError))
        {
            return ApplicationResult<FactorSearchRequest>.Failure(categoryError!);
        }

        if (!TryReadOptionalString(queryCollection, "activity", out var activity, out var activityError))
        {
            return ApplicationResult<FactorSearchRequest>.Failure(activityError!);
        }

        if (!TryReadOptionalString(queryCollection, "region", out var region, out var regionError))
        {
            return ApplicationResult<FactorSearchRequest>.Failure(regionError!);
        }

        if (!TryParseOptionalInt(queryCollection, "year", out var year, out var yearError))
        {
            return ApplicationResult<FactorSearchRequest>.Failure(yearError!);
        }

        if (!TryParseOptionalInt(queryCollection, "offset", out var offset, out var offsetError))
        {
            return ApplicationResult<FactorSearchRequest>.Failure(offsetError!);
        }

        if (!TryParseOptionalInt(queryCollection, "limit", out var limit, out var limitError))
        {
            return ApplicationResult<FactorSearchRequest>.Failure(limitError!);
        }

        var query = new FactorQuery(
            Category: category,
            Activity: activity,
            Region: region,
            Year: year);
        var pagination = new FactorPaginationQuery(Offset: offset, Limit: limit);

        var request = new FactorSearchRequest(query, pagination, queryCollection.Keys.ToArray());
        return ApplicationResult<FactorSearchRequest>.Success(request);
    }

    private static ApplicationResult<FactorListRequest> TryBuildPaginationQuery(
        IQueryCollection queryCollection,
        IReadOnlyCollection<string> supportedFilters)
    {
        var unsupportedFilters = queryCollection.Keys
            .Where(key => !supportedFilters.Contains(key, StringComparer.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        if (unsupportedFilters.Length > 0)
        {
            return ApplicationResult<FactorListRequest>.Failure(
                ApiError.InvalidQuery($"unsupported filters: {string.Join(", ", unsupportedFilters)}"));
        }

        if (!TryParseOptionalInt(queryCollection, "offset", out var offset, out var offsetError))
        {
            return ApplicationResult<FactorListRequest>.Failure(offsetError!);
        }

        if (!TryParseOptionalInt(queryCollection, "limit", out var limit, out var limitError))
        {
            return ApplicationResult<FactorListRequest>.Failure(limitError!);
        }

        return ApplicationResult<FactorListRequest>.Success(
            new FactorListRequest(
                new FactorPaginationQuery(Offset: offset, Limit: limit),
                queryCollection.Keys.ToArray()));
    }

    private static ApiError? ValidateFactorId(string factorId)
    {
        if (string.IsNullOrWhiteSpace(factorId))
        {
            return ApiError.InvalidQuery("factorId is required");
        }

        if (factorId.Any(char.IsWhiteSpace))
        {
            return ApiError.InvalidQuery("factorId must not contain whitespace");
        }

        return null;
    }

    private static bool TryParseOptionalInt(
        IQueryCollection queryCollection,
        string key,
        out int? value,
        out ApiError? error)
    {
        value = null;
        error = null;

        if (!TryReadOptionalString(queryCollection, key, out var rawValue, out error))
        {
            return false;
        }

        if (rawValue is null)
        {
            return true;
        }

        if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            error = ApiError.InvalidQuery($"{key} must be an integer");
            return false;
        }

        value = parsedValue;
        return true;
    }

    private static bool TryReadOptionalString(
        IQueryCollection queryCollection,
        string key,
        out string? value,
        out ApiError? error)
    {
        value = null;
        error = null;

        if (!queryCollection.TryGetValue(key, out var values))
        {
            return true;
        }

        if (values.Count > 1)
        {
            error = ApiError.InvalidQuery($"{key} must be provided once");
            return false;
        }

        if (IsMissingValue(values))
        {
            error = ApiError.InvalidQuery($"{key} must not be empty");
            return false;
        }

        value = values[0]!;
        return true;
    }

    private static bool IsMissingValue(StringValues values)
    {
        return values.Count == 0 || string.IsNullOrWhiteSpace(values[0]);
    }

    private sealed record FactorListRequest(
        FactorPaginationQuery Query,
        IReadOnlyCollection<string> RequestedFilterNames);

    private sealed record ImportAuthenticationContext(string TenantId, string AuthenticationScheme);

    private sealed record FactorSearchRequest(
        FactorQuery Query,
        FactorPaginationQuery Pagination,
        IReadOnlyCollection<string> RequestedFilterNames);
}
