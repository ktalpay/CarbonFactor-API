namespace CarbonOps.Infrastructure;

public interface IPostgreSqlSchemaExecutor
{
    Task ExecuteAsync(IReadOnlyList<PostgreSqlSchemaScript> scripts, CancellationToken cancellationToken = default);
}
