using CarbonOps.Contracts;
using CarbonOps.Domain;

namespace CarbonOps.Application.Factors;

public sealed class CarbonFactorUseCases
{
    private static readonly HashSet<string> AllowedFilters = new(StringComparer.Ordinal)
    {
        "category",
        "activity",
        "region",
        "year"
    };

    private readonly ICarbonFactorRepository repository;

    public CarbonFactorUseCases(ICarbonFactorRepository repository)
    {
        this.repository = repository;
    }

    public FactorListResponse ListCarbonFactors()
    {
        var factors = repository
            .ListCarbonFactors()
            .OrderBy(factor => factor.Id, StringComparer.Ordinal)
            .Select(CarbonFactorMapper.ToDto)
            .ToList();

        return new FactorListResponse(factors, factors.Count);
    }

    public ApplicationResult<FactorDetailResponse> GetCarbonFactorById(string factorId)
    {
        if (string.IsNullOrWhiteSpace(factorId))
        {
            return ApplicationResult<FactorDetailResponse>.Failure(
                ApiError.InvalidQuery("factor id is required"));
        }

        var factor = repository.GetCarbonFactorById(factorId);
        if (factor is null)
        {
            return ApplicationResult<FactorDetailResponse>.Failure(ApiError.NotFound("factor", factorId));
        }

        return ApplicationResult<FactorDetailResponse>.Success(
            new FactorDetailResponse(CarbonFactorMapper.ToDto(factor)));
    }

    public ApplicationResult<FactorListResponse> SearchCarbonFactors(
        FactorQuery query,
        IReadOnlyCollection<string>? requestedFilterNames = null)
    {
        if (query.Year is <= 0)
        {
            return ApplicationResult<FactorListResponse>.Failure(ApiError.InvalidQuery("year must be positive"));
        }

        var unsupportedFilters = UnsupportedFilters(requestedFilterNames);
        if (unsupportedFilters.Count > 0)
        {
            return ApplicationResult<FactorListResponse>.Failure(
                ApiError.InvalidQuery($"unsupported filters: {string.Join(", ", unsupportedFilters)}"));
        }

        IEnumerable<CarbonFactor> factors = repository.ListCarbonFactors();

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            factors = factors.Where(factor => factor.Category == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Activity))
        {
            factors = factors.Where(factor => factor.Activity == query.Activity);
        }

        if (!string.IsNullOrWhiteSpace(query.Region))
        {
            factors = factors.Where(factor => factor.Region == query.Region);
        }

        if (query.Year is not null)
        {
            factors = factors.Where(factor => factor.Year == query.Year);
        }

        var factorDtos = factors
            .OrderBy(factor => factor.Id, StringComparer.Ordinal)
            .Select(CarbonFactorMapper.ToDto)
            .ToList();

        return ApplicationResult<FactorListResponse>.Success(
            new FactorListResponse(factorDtos, factorDtos.Count));
    }

    private static IReadOnlyList<string> UnsupportedFilters(IReadOnlyCollection<string>? requestedFilterNames)
    {
        if (requestedFilterNames is null || requestedFilterNames.Count == 0)
        {
            return [];
        }

        return requestedFilterNames
            .Where(filter => !AllowedFilters.Contains(filter))
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
