using System.Text.Json.Serialization;

namespace CarbonOps.Contracts;

public sealed record FactorQuery(
    [property: JsonPropertyName("category")] string? Category = null,
    [property: JsonPropertyName("activity")] string? Activity = null,
    [property: JsonPropertyName("region")] string? Region = null,
    [property: JsonPropertyName("year")] int? Year = null);
