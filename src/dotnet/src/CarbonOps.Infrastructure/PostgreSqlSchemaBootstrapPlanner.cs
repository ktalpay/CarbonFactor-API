namespace CarbonOps.Infrastructure;

public interface IPostgreSqlSchemaBootstrapPlanner
{
    IReadOnlyList<PostgreSqlSchemaScript> CreatePlan();
}

public sealed class PostgreSqlSchemaBootstrapPlanner : IPostgreSqlSchemaBootstrapPlanner
{
    private readonly IPostgreSqlSchemaScriptCatalog _scriptCatalog;
    private readonly PostgreSqlSchemaSafetyValidator _safetyValidator;

    public PostgreSqlSchemaBootstrapPlanner(
        IPostgreSqlSchemaScriptCatalog scriptCatalog,
        PostgreSqlSchemaSafetyValidator safetyValidator)
    {
        _scriptCatalog = scriptCatalog;
        _safetyValidator = safetyValidator;
    }

    public IReadOnlyList<PostgreSqlSchemaScript> CreatePlan()
    {
        var scripts = _scriptCatalog.GetScripts();
        _safetyValidator.ValidateScripts(scripts);
        return scripts;
    }
}
