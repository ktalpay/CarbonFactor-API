using System.Text.Json.Serialization;

namespace CarbonOps.Contracts;

public sealed record FactorPaginationQuery(
    [property: JsonPropertyName("offset")] int? Offset = null,
    [property: JsonPropertyName("limit")] int? Limit = null);
