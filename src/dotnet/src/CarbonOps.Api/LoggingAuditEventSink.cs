namespace CarbonOps.Api;

internal sealed class LoggingAuditEventSink : IAuditEventSink
{
    private const string LoggerCategory = "CarbonOps.Api.Audit";

    private readonly ILogger logger;

    public LoggingAuditEventSink(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        logger = loggerFactory.CreateLogger(LoggerCategory);
    }

    public ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        logger.Log(
            ResolveLogLevel(auditEvent),
            "CarbonOps audit event {event_id} {event_type} {occurred_at_utc} {severity} {endpoint} {correlation_id} {authentication_scheme} {tenant_id} {outcome} {reason_code} {batch_id} {validation_status} {accepted_records} {rejected_records} {error_count} {warning_count} {persisted} {import_execution}",
            auditEvent.EventId,
            auditEvent.EventType,
            auditEvent.OccurredAtUtc,
            auditEvent.Severity,
            auditEvent.Endpoint,
            auditEvent.CorrelationId,
            auditEvent.AuthenticationScheme,
            auditEvent.TenantId,
            auditEvent.Outcome,
            auditEvent.ReasonCode,
            auditEvent.BatchId,
            auditEvent.ValidationStatus,
            auditEvent.AcceptedRecords,
            auditEvent.RejectedRecords,
            auditEvent.ErrorCount,
            auditEvent.WarningCount,
            auditEvent.Persisted,
            auditEvent.ImportExecution);

        return ValueTask.CompletedTask;
    }

    private static LogLevel ResolveLogLevel(AuditEvent auditEvent)
    {
        return auditEvent.Severity switch
        {
            AuditEventSeverity.Warning => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}
