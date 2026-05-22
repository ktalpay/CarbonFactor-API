namespace CarbonOps.Infrastructure;

public sealed record PostgreSqlSchemaBootstrapResult(
    PostgreSqlSchemaBootstrapMode Mode,
    int ScriptsPlanned,
    int ScriptsExecuted,
    string ValidationStatus);
