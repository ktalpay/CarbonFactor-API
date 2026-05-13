using System.Text.Json.Serialization;

namespace CarbonOps.Contracts;

public sealed record FactorDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("activity")] string Activity,
    [property: JsonPropertyName("factor_value")] decimal FactorValue,
    [property: JsonPropertyName("factor_unit")] string FactorUnit,
    [property: JsonPropertyName("region")] string? Region = null,
    [property: JsonPropertyName("year")] int? Year = null,
    [property: JsonPropertyName("notes")] string? Notes = null);
