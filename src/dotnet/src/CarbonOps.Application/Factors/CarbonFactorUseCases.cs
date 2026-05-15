using CarbonOps.Contracts;
using CarbonOps.Domain;

namespace CarbonOps.Application.Factors;

public sealed class CarbonFactorUseCases
{
    private static readonly HashSet<string> AllowedSearchFilters = new(StringComparer.Ordinal)
    {
        "category",
        "activity",
        "region",
        "year",
        "offset",
        "limit"
    };

    private static readonly HashSet<string> AllowedListFilters = new(StringComparer.Ordinal)
    {
        "offset",
        "limit"
    };

    private readonly ICarbonFactorRepository repository;

    public CarbonFactorUseCases(ICarbonFactorRepository repository)
    {
        this.repository = repository;
    }

    public FactorListResponse ListCarbonFactors()
    {
        return BuildFactorListResponse(repository.ListCarbonFactors(), new FactorPaginationQuery());
    }

    public ApplicationResult<FactorListResponse> ListCarbonFactors(
        FactorPaginationQuery pagination,
        IReadOnlyCollection<string>? requestedFilterNames = null)
    {
        var unsupportedFilters = UnsupportedFilters(requestedFilterNames, AllowedListFilters);
        if (unsupportedFilters.Count > 0)
        {
            return ApplicationResult<FactorListResponse>.Failure(
                ApiError.InvalidQuery($"unsupported filters: {string.Join(", ", unsupportedFilters)}"));
        }

        var paginationError = ValidatePagination(pagination);
        if (paginationError is not null)
        {
            return ApplicationResult<FactorListResponse>.Failure(paginationError);
        }

        return ApplicationResult<FactorListResponse>.Success(
            BuildFactorListResponse(repository.ListCarbonFactors(), pagination));
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
        FactorPaginationQuery? pagination = null,
        IReadOnlyCollection<string>? requestedFilterNames = null)
    {
        if (query.Year is <= 0)
        {
            return ApplicationResult<FactorListResponse>.Failure(ApiError.InvalidQuery("year must be positive"));
        }

        var unsupportedFilters = UnsupportedFilters(requestedFilterNames, AllowedSearchFilters);
        if (unsupportedFilters.Count > 0)
        {
            return ApplicationResult<FactorListResponse>.Failure(
                ApiError.InvalidQuery($"unsupported filters: {string.Join(", ", unsupportedFilters)}"));
        }

        pagination ??= new FactorPaginationQuery();

        var paginationError = ValidatePagination(pagination);
        if (paginationError is not null)
        {
            return ApplicationResult<FactorListResponse>.Failure(paginationError);
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

        return ApplicationResult<FactorListResponse>.Success(
            BuildFactorListResponse(factors, pagination));
    }

    private static FactorListResponse BuildFactorListResponse(
        IEnumerable<CarbonFactor> factors,
        FactorPaginationQuery pagination)
    {
        var orderedFactors = factors
            .OrderBy(factor => factor.Id, StringComparer.Ordinal)
            .Select(CarbonFactorMapper.ToDto)
            .ToList();

        var pagedFactors = orderedFactors
            .Skip(pagination.Offset ?? 0)
            .Take(pagination.Limit ?? orderedFactors.Count)
            .ToList();

        return new FactorListResponse(pagedFactors, orderedFactors.Count);
    }

    private static ApiError? ValidatePagination(FactorPaginationQuery pagination)
    {
        if (pagination.Offset is < 0)
        {
            return ApiError.InvalidQuery("offset must be zero or positive");
        }

        if (pagination.Limit is <= 0)
        {
            return ApiError.InvalidQuery("limit must be positive");
        }

        return null;
    }

    private static IReadOnlyList<string> UnsupportedFilters(
        IReadOnlyCollection<string>? requestedFilterNames,
        HashSet<string> allowedFilters)
    {
        if (requestedFilterNames is null || requestedFilterNames.Count == 0)
        {
            return [];
        }

        return requestedFilterNames
            .Where(filter => !allowedFilters.Contains(filter))
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
