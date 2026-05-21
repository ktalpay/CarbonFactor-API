namespace CarbonOps.Infrastructure;

public interface IPostgreSqlSchemaScriptCatalog
{
    IReadOnlyList<PostgreSqlSchemaScript> GetScripts();
}
