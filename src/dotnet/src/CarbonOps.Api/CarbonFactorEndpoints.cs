using System.Globalization;
using CarbonOps.Application.Factors;
using CarbonOps.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace CarbonOps.Api;

internal static class CarbonFactorEndpoints
{
    private const string ImportEndpointRequiredScope = "carbon_factors:import";

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
        var group = endpoints.MapGroup("/carbon-factors");

        group.MapGet("/", (HttpRequest request, CarbonFactorUseCases useCases) =>
            ListCarbonFactors(request, useCases))
            .WithMetadata(CarbonFactorEndpointExamples.ListFactorsSuccess);

        group.MapGet("/search", (HttpRequest request, CarbonFactorUseCases useCases) =>
            SearchCarbonFactors(request, useCases))
            .WithMetadata(
                CarbonFactorEndpointExamples.SearchFactorsSuccess,
                CarbonFactorEndpointExamples.SearchFactorsInvalidQuery);

        group.MapGet("/{factorId}", (string factorId, CarbonFactorUseCases useCases) =>
            GetCarbonFactorById(factorId, useCases))
            .WithMetadata(
                CarbonFactorEndpointExamples.GetFactorByIdSuccess,
                CarbonFactorEndpointExamples.GetFactorByIdNotFound);

        group.MapPost("/import", (HttpRequest httpRequest, ParserCarbonFactorBatchImportRequest request, CarbonFactorImportBoundaryService boundaryService, IOptions<ApiKeyAuthenticationOptions> apiKeyOptions) =>
            ImportCarbonFactors(httpRequest, request, boundaryService, apiKeyOptions.Value));

        return endpoints;
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

    private static IResult ImportCarbonFactors(
        HttpRequest httpRequest,
        ParserCarbonFactorBatchImportRequest request,
        CarbonFactorImportBoundaryService boundaryService,
        ApiKeyAuthenticationOptions apiKeyOptions)
    {
        var importAuthContext = EnsureAuthorized(httpRequest, apiKeyOptions).GetValueOrThrow();

        var result = boundaryService.ValidateAndAccept(request).GetValueOrThrow();
        var scopedResult = result with
        {
            Audit = result.Audit with
            {
                TenantId = importAuthContext.TenantId,
                AuthenticationScheme = importAuthContext.AuthenticationScheme
            }
        };

        return TypedResults.Accepted($"/carbon-factors/import/{scopedResult.BatchId}", scopedResult);
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

        if (!request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var providedApiKey)
            || string.IsNullOrWhiteSpace(providedApiKey))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("missing API key"));
        }

        if (!ApiKeyHashVerifier.VerifySha256Hex(providedApiKey.ToString(), configuredKeyHash))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("invalid API key"));
        }

        if (string.IsNullOrWhiteSpace(options.ImportTenantId))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("import tenant is not configured"));
        }

        var configuredScopes = options.ImportEndpointScopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .ToArray();

        if (configuredScopes.Length == 0)
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("import endpoint scope is not configured"));
        }

        if (!configuredScopes.Contains(ImportEndpointRequiredScope, StringComparer.Ordinal))
        {
            return ApplicationResult<ImportAuthenticationContext>.Failure(ApiError.Unauthorized("API key is not permitted to import carbon factors"));
        }

        return ApplicationResult<ImportAuthenticationContext>.Success(
            new ImportAuthenticationContext(options.ImportTenantId.Trim(), "api_key"));
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
