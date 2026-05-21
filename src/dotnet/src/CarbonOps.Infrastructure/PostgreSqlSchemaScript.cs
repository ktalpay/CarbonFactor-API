namespace CarbonOps.Infrastructure;

public sealed record PostgreSqlSchemaScript(string Name, string RelativePath, string Sql);
