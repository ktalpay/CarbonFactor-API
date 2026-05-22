namespace CarbonOps.Infrastructure;

public interface IPostgreSqlSchemaBootstrapper
{
    Task<PostgreSqlSchemaBootstrapResult> BootstrapAsync(
        PostgreSqlSchemaBootstrapMode mode,
        CancellationToken cancellationToken = default);
}
