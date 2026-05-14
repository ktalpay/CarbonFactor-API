using System.Globalization;
using CarbonOps.Application.Factors;
using CarbonOps.Contracts;

namespace CarbonOps.Api;

internal static class CarbonFactorEndpoints
{
    public static IEndpointRouteBuilder MapCarbonFactorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/carbon-factors");

        group.MapGet("/", (CarbonFactorUseCases useCases) =>
            TypedResults.Ok(useCases.ListCarbonFactors()))
            .WithMetadata(CarbonFactorEndpointExamples.ListFactorsSuccess);

        group.MapGet("/search", (HttpRequest request, CarbonFactorUseCases useCases) =>
            SearchCarbonFactors(request, useCases))
            .WithMetadata(
                CarbonFactorEndpointExamples.SearchFactorsSuccess,
                CarbonFactorEndpointExamples.SearchFactorsInvalidQuery);

        group.MapGet("/{factorId}", (string factorId, CarbonFactorUseCases useCases) =>
            useCases.GetCarbonFactorById(factorId).ToHttpResult())
            .WithMetadata(
                CarbonFactorEndpointExamples.GetFactorByIdSuccess,
                CarbonFactorEndpointExamples.GetFactorByIdNotFound);

        return endpoints;
    }

    private static IResult SearchCarbonFactors(HttpRequest request, CarbonFactorUseCases useCases)
    {
        var queryResult = TryBuildFactorQuery(request.Query);
        if (!queryResult.IsSuccess)
        {
            return queryResult.ToHttpResult();
        }

        return useCases
            .SearchCarbonFactors(queryResult.Value!.Query, queryResult.Value.RequestedFilterNames)
            .ToHttpResult();
    }

    private static ApplicationResult<FactorSearchRequest> TryBuildFactorQuery(IQueryCollection queryCollection)
    {
        if (!TryParseOptionalInt(queryCollection, "year", out var year, out var parseError))
        {
            return ApplicationResult<FactorSearchRequest>.Failure(parseError!);
        }

        var query = new FactorQuery(
            Category: ReadOptionalValue(queryCollection, "category"),
            Activity: ReadOptionalValue(queryCollection, "activity"),
            Region: ReadOptionalValue(queryCollection, "region"),
            Year: year);

        var request = new FactorSearchRequest(query, queryCollection.Keys.ToArray());
        return ApplicationResult<FactorSearchRequest>.Success(request);
    }

    private static bool TryParseOptionalInt(
        IQueryCollection queryCollection,
        string key,
        out int? value,
        out ApiError? error)
    {
        value = null;
        error = null;

        var rawValue = ReadOptionalValue(queryCollection, key);
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

    private static string? ReadOptionalValue(IQueryCollection queryCollection, string key)
    {
        if (!queryCollection.TryGetValue(key, out var values))
        {
            return null;
        }

        var value = values.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private sealed record FactorSearchRequest(FactorQuery Query, IReadOnlyCollection<string> RequestedFilterNames);
}
