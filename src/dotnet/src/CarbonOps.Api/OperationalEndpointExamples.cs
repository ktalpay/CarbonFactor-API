namespace CarbonOps.Api;

public static class OperationalEndpointExamples
{
    public static EndpointExample HealthSuccess { get; } = new(
        "health-success",
        "/health",
        StatusCodes.Status200OK,
        """
        {
          "status": "ok"
        }
        """);

    public static EndpointExample LivenessSuccess { get; } = new(
        "health-live-success",
        "/health/live",
        StatusCodes.Status200OK,
        """
        {
          "status": "ok",
          "check": "liveness"
        }
        """);

    public static EndpointExample ReadinessSuccess { get; } = new(
        "health-ready-success",
        "/health/ready",
        StatusCodes.Status200OK,
        """
        {
          "status": "ok",
          "check": "readiness"
        }
        """);

    public static EndpointExample VersionSuccess { get; } = new(
        "version-success",
        "/version",
        StatusCodes.Status200OK,
        """
        {
          "name": "CarbonOps API",
          "version": "0.1.0"
        }
        """);
}
