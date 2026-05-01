namespace CarbonFactor.Api.Contracts;

public sealed record CarbonFactorBatchIngestRequest(
    IReadOnlyList<CarbonFactorCreateRequest>? Items);

