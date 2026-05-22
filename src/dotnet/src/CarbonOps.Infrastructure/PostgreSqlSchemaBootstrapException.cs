namespace CarbonOps.Infrastructure;

public sealed class PostgreSqlSchemaBootstrapException : InvalidOperationException
{
    public PostgreSqlSchemaBootstrapException(string message)
        : base(message)
    {
    }
}
