namespace CarbonFactor.Api.Contracts;

public sealed record CarbonFactorIngestionResponse(
    int Total,
    int Accepted,
    int Rejected,
    int WarningCount,
    IReadOnlyList<CarbonFactorIngestionItemResult> Items);

