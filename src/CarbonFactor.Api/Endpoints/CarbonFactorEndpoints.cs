using CarbonFactor.Api.Contracts;
using CarbonFactor.Api.Domain;
using CarbonFactor.Api.Errors;
using CarbonFactor.Api.Storage;

namespace CarbonFactor.Api.Endpoints;

public static class CarbonFactorEndpoints
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapCarbonFactorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/carbon-factors")
            .WithTags("Carbon Factors");

        group.MapGet("/", Query)
            .WithName("QueryCarbonFactors")
            .WithSummary("Queries carbon factors using optional filters and pagination.")
            .Produces<CarbonFactorQueryResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/{id}", GetById)
            .WithName("GetCarbonFactorById")
            .WithSummary("Gets a carbon factor by identifier.")
            .Produces<CarbonFactorResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateCarbonFactor")
            .WithSummary("Creates a single carbon factor record.")
            .Accepts<CarbonFactorCreateRequest>("application/json")
            .Produces<CarbonFactorResponse>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/batch", BatchIngest)
            .WithName("BatchIngestCarbonFactors")
            .WithSummary("Validates and ingests a batch of carbon factor records.")
            .Accepts<CarbonFactorBatchIngestRequest>("application/json")
            .Produces<CarbonFactorIngestionResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static IResult Query(
        ICarbonFactorStore store,
        CarbonFactorNormalizer normalizer,
        HttpContext context,
        string? category = null,
        string? unit = null,
        string? source = null,
        string? region = null,
        int? effectiveYear = null,
        string? search = null,
        int? page = null,
        int? pageSize = null)
    {
        var errors = ValidateQuery(normalizer, category, unit, page, pageSize);
        if (errors.Count > 0)
        {
            return Results.BadRequest(ApiErrorResponse.Create(
                StatusCodes.Status400BadRequest,
                "validation_failed",
                "Validation failed.",
                "One or more query parameters failed validation.",
                context.TraceIdentifier,
                errors));
        }

        var normalizedCategory = normalizer.NormalizeCategory(category);
        var normalizedUnit = normalizer.NormalizeUnit(unit);
        var normalizedSource = normalizer.NormalizeOptionalText(source);
        var normalizedRegion = normalizer.NormalizeOptionalText(region);
        var normalizedSearch = normalizer.NormalizeOptionalText(search);
        var currentPage = page ?? DefaultPage;
        var currentPageSize = pageSize ?? DefaultPageSize;

        var query = store.List().AsEnumerable();

        if (normalizedCategory is not null)
        {
            query = query.Where(item => item.Category == normalizedCategory);
        }

        if (normalizedUnit is not null)
        {
            query = query.Where(item => item.Unit == normalizedUnit);
        }

        if (normalizedSource is not null)
        {
            query = query.Where(item => string.Equals(item.Source, normalizedSource, StringComparison.OrdinalIgnoreCase));
        }

        if (normalizedRegion is not null)
        {
            query = query.Where(item => string.Equals(item.Region, normalizedRegion, StringComparison.OrdinalIgnoreCase));
        }

        if (effectiveYear is not null)
        {
            query = query.Where(item => item.EffectiveYear == effectiveYear);
        }

        if (normalizedSearch is not null)
        {
            query = query.Where(item => item.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id)
            .ToArray();

        var totalCount = ordered.Length;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)currentPageSize);
        var items = ordered
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .Select(ToResponse)
            .ToArray();

        return Results.Ok(new CarbonFactorQueryResponse(
            currentPage,
            currentPageSize,
            totalCount,
            totalPages,
            items));
    }

    private static IResult GetById(string id, ICarbonFactorStore store, HttpContext context)
    {
        if (!Guid.TryParse(id, out var factorId))
        {
            return Results.BadRequest(ApiErrorResponse.Create(
                StatusCodes.Status400BadRequest,
                "invalid_identifier",
                "Invalid carbon factor identifier.",
                "The carbon factor identifier must be a valid GUID.",
                context.TraceIdentifier,
                [new ApiValidationError("id", "invalid_guid", "Identifier must be a valid GUID.")]));
        }

        var factor = store.Get(factorId);
        if (factor is null)
        {
            return Results.NotFound(ApiErrorResponse.Create(
                StatusCodes.Status404NotFound,
                "carbon_factor_not_found",
                "Carbon factor not found.",
                "No carbon factor record was found for the supplied identifier.",
                context.TraceIdentifier));
        }

        return Results.Ok(ToResponse(factor));
    }

    private static IResult Create(
        CarbonFactorCreateRequest? request,
        ICarbonFactorStore store,
        CarbonFactorValidator validator,
        HttpContext context)
    {
        if (request is null)
        {
            return Results.BadRequest(ApiErrorResponse.Create(
                StatusCodes.Status400BadRequest,
                "validation_failed",
                "Validation failed.",
                "One or more request fields failed validation.",
                context.TraceIdentifier,
                [new ApiValidationError("body", "required", "Request body is required.")]));
        }

        var result = validator.Create(Guid.NewGuid(), ToInput(request));
        if (!result.IsValid)
        {
            return Results.BadRequest(ApiErrorResponse.Create(
                StatusCodes.Status400BadRequest,
                "validation_failed",
                "Validation failed.",
                "One or more request fields failed validation.",
                context.TraceIdentifier,
                result.Errors.Select(ToApiError).ToArray()));
        }

        var factor = store.Add(result.Factor!);

        return Results.Created($"/api/carbon-factors/{factor.Id}", ToResponse(factor));
    }

    private static IResult BatchIngest(
        CarbonFactorBatchIngestRequest? request,
        ICarbonFactorStore store,
        CarbonFactorValidator validator,
        HttpContext context)
    {
        if (request?.Items is null)
        {
            return Results.BadRequest(ApiErrorResponse.Create(
                StatusCodes.Status400BadRequest,
                "validation_failed",
                "Validation failed.",
                "One or more request fields failed validation.",
                context.TraceIdentifier,
                [new ApiValidationError("items", "required", "Items are required.")]));
        }

        if (request.Items.Count == 0)
        {
            return Results.BadRequest(ApiErrorResponse.Create(
                StatusCodes.Status400BadRequest,
                "validation_failed",
                "Validation failed.",
                "One or more request fields failed validation.",
                context.TraceIdentifier,
                [new ApiValidationError("items", "empty_batch", "Batch must contain at least one item.")]));
        }

        var results = new List<CarbonFactorIngestionItemResult>(request.Items.Count);
        var accepted = 0;
        var rejected = 0;

        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];
            var validation = validator.Create(Guid.NewGuid(), ToInput(item));
            if (!validation.IsValid)
            {
                rejected++;
                results.Add(new CarbonFactorIngestionItemResult(
                    index,
                    "rejected",
                    null,
                    validation.Errors.Select(ToApiError).ToArray()));
                continue;
            }

            var record = store.Add(validation.Factor!);
            accepted++;
            results.Add(new CarbonFactorIngestionItemResult(
                index,
                "accepted",
                record.Id.ToString("D"),
                []));
        }

        var response = new CarbonFactorIngestionResponse(
            request.Items.Count,
            accepted,
            rejected,
            WarningCount: 0,
            results);

        return Results.Ok(response);
    }

    private static CarbonFactorResponse ToResponse(CarbonFactorRecord factor) =>
        new(
            factor.Id.ToString("D"),
            factor.Name,
            factor.Category,
            factor.Unit,
            factor.EmissionValue,
            factor.Source,
            factor.Region,
            factor.EffectiveYear);

    private static CarbonFactorInput ToInput(CarbonFactorCreateRequest request) =>
        new(
            request.Name,
            request.Category,
            request.Unit,
            request.EmissionValue,
            request.Source,
            request.Region,
            request.EffectiveYear);

    private static ApiValidationError ToApiError(CarbonFactorValidationError error) =>
        new(error.Field, error.Code, error.Message);

    private static IReadOnlyList<ApiValidationError> ValidateQuery(
        CarbonFactorNormalizer normalizer,
        string? category,
        string? unit,
        int? page,
        int? pageSize)
    {
        var errors = new List<ApiValidationError>();
        var normalizedCategory = normalizer.NormalizeCategory(category);
        var normalizedUnit = normalizer.NormalizeUnit(unit);

        if (normalizedCategory is not null && !CarbonFactorNormalizer.SupportedCategories.Contains(normalizedCategory))
        {
            errors.Add(new ApiValidationError("category", "unsupported_category", "Unsupported carbon factor category."));
        }

        if (normalizedUnit is not null && !CarbonFactorNormalizer.SupportedUnits.Contains(normalizedUnit))
        {
            errors.Add(new ApiValidationError("unit", "unsupported_unit", "Unsupported emission factor unit."));
        }

        if (page is <= 0)
        {
            errors.Add(new ApiValidationError("page", "invalid_range", "Page must be greater than zero."));
        }

        if (pageSize is <= 0)
        {
            errors.Add(new ApiValidationError("pageSize", "invalid_range", "Page size must be greater than zero."));
        }
        else if (pageSize > MaxPageSize)
        {
            errors.Add(new ApiValidationError("pageSize", "invalid_range", $"Page size must be less than or equal to {MaxPageSize}."));
        }

        return errors;
    }
}
