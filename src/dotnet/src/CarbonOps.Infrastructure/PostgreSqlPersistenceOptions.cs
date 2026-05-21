namespace CarbonOps.Infrastructure;

public sealed class PostgreSqlPersistenceOptions
{
    public const string SectionName = "Persistence:PostgreSql";

    public string? ConnectionString { get; init; }
}
