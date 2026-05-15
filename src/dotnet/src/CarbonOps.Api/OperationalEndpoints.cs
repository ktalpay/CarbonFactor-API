namespace CarbonOps.Api;

internal static class OperationalEndpoints
{
    internal const string ApiName = "CarbonOps API";
    internal const string ApiVersion = "0.1.0";

    public static IEndpointRouteBuilder MapOperationalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => TypedResults.Ok(new
        {
            status = "ok"
        }))
        .WithMetadata(OperationalEndpointExamples.HealthSuccess);

        endpoints.MapGet("/health/live", () => TypedResults.Ok(new
        {
            status = "ok",
            check = "liveness"
        }))
        .WithMetadata(OperationalEndpointExamples.LivenessSuccess);

        endpoints.MapGet("/health/ready", () => TypedResults.Ok(new
        {
            status = "ok",
            check = "readiness"
        }))
        .WithMetadata(OperationalEndpointExamples.ReadinessSuccess);

        endpoints.MapGet("/version", () => TypedResults.Ok(new
        {
            name = ApiName,
            version = ApiVersion
        }))
        .WithMetadata(OperationalEndpointExamples.VersionSuccess);

        return endpoints;
    }
}
