using System.Text.Json.Serialization;

namespace CarbonOps.Contracts;

public sealed record FactorListResponse(
    [property: JsonPropertyName("factors")] IReadOnlyList<FactorDto> Factors,
    [property: JsonPropertyName("total")] int Total);
