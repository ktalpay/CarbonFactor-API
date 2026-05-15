using System.Globalization;
using CarbonOps.Application.Factors;
using CarbonOps.Contracts;
using Microsoft.Extensions.Primitives;

namespace CarbonOps.Api;

internal static class CarbonFactorEndpoints
{
    private static readonly string[] SupportedFilters =
    [
        "category",
        "activity",
        "region",
        "year"
    ];

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
            GetCarbonFactorById(factorId, useCases))
            .WithMetadata(
                CarbonFactorEndpointExamples.GetFactorByIdSuccess,
                CarbonFactorEndpointExamples.GetFactorByIdNotFound);

        return endpoints;
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
                .SearchCarbonFactors(queryRequest.Query, queryRequest.RequestedFilterNames)
                .GetValueOrThrow());
    }

    private static ApplicationResult<FactorSearchRequest> TryBuildFactorQuery(IQueryCollection queryCollection)
    {
        var unsupportedFilters = queryCollection.Keys
            .Where(key => !SupportedFilters.Contains(key, StringComparer.Ordinal))
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

        var query = new FactorQuery(
            Category: category,
            Activity: activity,
            Region: region,
            Year: year);

        var request = new FactorSearchRequest(query, queryCollection.Keys.ToArray());
        return ApplicationResult<FactorSearchRequest>.Success(request);
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

    private sealed record FactorSearchRequest(FactorQuery Query, IReadOnlyCollection<string> RequestedFilterNames);
}
