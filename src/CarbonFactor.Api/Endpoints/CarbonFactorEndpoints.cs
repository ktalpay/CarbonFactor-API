using CarbonFactor.Api.Contracts;
using CarbonFactor.Api.Domain;
using CarbonFactor.Api.Errors;
using CarbonFactor.Api.Storage;

namespace CarbonFactor.Api.Endpoints;

public static class CarbonFactorEndpoints
{
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
}
