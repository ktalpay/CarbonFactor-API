namespace CarbonFactor.Api.Contracts;

public sealed record CarbonFactorQueryResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<CarbonFactorResponse> Items);

