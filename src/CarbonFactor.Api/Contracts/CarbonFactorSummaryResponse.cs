namespace CarbonFactor.Api.Contracts;

public sealed record CarbonFactorSummaryResponse(
    int TotalCount,
    IReadOnlyList<CarbonFactorSummaryCount> CountByCategory,
    IReadOnlyList<CarbonFactorSummaryCount> CountByUnit,
    IReadOnlyList<CarbonFactorSummaryCount> CountBySource,
    IReadOnlyList<CarbonFactorSummaryCount> CountByRegion,
    int? MinEffectiveYear,
    int? MaxEffectiveYear);

