namespace CarbonOps.Api;

internal interface IAuditEventSink
{
    ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
