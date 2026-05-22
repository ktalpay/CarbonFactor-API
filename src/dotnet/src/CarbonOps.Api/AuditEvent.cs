namespace CarbonOps.Api;

internal sealed record AuditEvent(
    string EventId,
    string EventType,
    DateTimeOffset OccurredAtUtc,
    string Severity,
    string Endpoint,
    string? CorrelationId,
    string Outcome,
    string? ReasonCode = null,
    string? AuthenticationScheme = null,
    string? TenantId = null,
    string? BatchId = null,
    string? ValidationStatus = null,
    int? AcceptedRecords = null,
    int? RejectedRecords = null,
    int? ErrorCount = null,
    int? WarningCount = null,
    bool? Persisted = null,
    string? ImportExecution = null);

internal static class AuditEventTypes
{
    public const string ImportAuthorizationFailed = "import.authorization_failed";
    public const string ImportValidationFailed = "import.validation_failed";
    public const string ImportAccepted = "import.accepted";
}

internal static class AuditEventSeverity
{
    public const string Information = "Information";
    public const string Warning = "Warning";
}

internal static class AuditEventOutcomes
{
    public const string Success = "success";
    public const string Failure = "failure";
}
