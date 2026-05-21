using System.Text.Json.Serialization;

namespace CarbonOps.Contracts;

public sealed record ApiError(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("details")] IReadOnlyDictionary<string, object> Details)
{
    public static ApiError NotFound(string entity, string identifier)
    {
        return new ApiError(
            "not_found",
            $"{entity} not found",
            new Dictionary<string, object> { ["id"] = identifier });
    }

    public static ApiError InvalidQuery(string reason)
    {
        return new ApiError(
            "invalid_query",
            "Invalid query",
            new Dictionary<string, object> { ["reason"] = reason });
    }

    public static ApiError Unauthorized(string reason)
    {
        return new ApiError(
            "unauthorized",
            "Unauthorized",
            new Dictionary<string, object> { ["reason"] = reason });
    }
}
