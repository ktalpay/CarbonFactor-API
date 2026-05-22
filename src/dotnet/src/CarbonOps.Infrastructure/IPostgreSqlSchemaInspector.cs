namespace CarbonOps.Infrastructure;

public interface IPostgreSqlSchemaInspector
{
    Task<PostgreSqlSchemaInspectionResult> InspectAsync(CancellationToken cancellationToken = default);
}
