using System.Text.Json.Serialization;

namespace CarbonOps.Contracts;

public sealed record FactorDetailResponse(
    [property: JsonPropertyName("factor")] FactorDto Factor);
