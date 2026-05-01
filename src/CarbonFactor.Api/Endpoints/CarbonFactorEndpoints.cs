using CarbonFactor.Api.Contracts;
using CarbonFactor.Api.Errors;
using CarbonFactor.Api.Storage;

namespace CarbonFactor.Api.Endpoints;

public static class CarbonFactorEndpoints
{
    private static readonly HashSet<string> SupportedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "energy",
        "transport",
        "materials",
        "waste",
        "agriculture",
        "water",
        "other"
    };

    private static readonly HashSet<string> SupportedUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "kg_co2e",
        "g_co2e",
        "t_co2e",
        "kg_co2e_per_kwh",
        "kg_co2e_per_liter",
        "kg_co2e_per_km",
        "kg_co2e_per_unit"
    };

    public static IEndpointRouteBuilder MapCarbonFactorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/carbon-factors")
            .WithTags("Carbon Factors");

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

        return app;
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

    private static IResult Create(CarbonFactorCreateRequest? request, ICarbonFactorStore store, HttpContext context)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Results.BadRequest(ApiErrorResponse.Create(
                StatusCodes.Status400BadRequest,
                "validation_failed",
                "Validation failed.",
                "One or more request fields failed validation.",
                context.TraceIdentifier,
                errors));
        }

        var factor = store.Add(request!);

        return Results.Created($"/api/carbon-factors/{factor.Id}", ToResponse(factor));
    }

    private static IReadOnlyList<ApiValidationError> Validate(CarbonFactorCreateRequest? request)
    {
        if (request is null)
        {
            return [new ApiValidationError("body", "required", "Request body is required.")];
        }

        var errors = new List<ApiValidationError>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ApiValidationError("name", "required", "Name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            errors.Add(new ApiValidationError("category", "required", "Category is required."));
        }
        else if (!SupportedCategories.Contains(request.Category.Trim()))
        {
            errors.Add(new ApiValidationError("category", "unsupported_category", "Unsupported carbon factor category."));
        }

        if (string.IsNullOrWhiteSpace(request.Unit))
        {
            errors.Add(new ApiValidationError("unit", "required", "Unit is required."));
        }
        else if (!SupportedUnits.Contains(request.Unit.Trim()))
        {
            errors.Add(new ApiValidationError("unit", "unsupported_unit", "Unsupported emission factor unit."));
        }

        if (request.EmissionValue is null)
        {
            errors.Add(new ApiValidationError("emissionValue", "required", "Emission value is required."));
        }
        else if (request.EmissionValue <= 0)
        {
            errors.Add(new ApiValidationError("emissionValue", "invalid_range", "Emission value must be greater than zero."));
        }

        return errors;
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
}

