namespace CarbonOps.Infrastructure;

public sealed record PostgreSqlSchemaInspectionResult(IReadOnlyList<string> MissingObjects)
{
    public bool IsValid => MissingObjects.Count == 0;

    public static PostgreSqlSchemaInspectionResult Valid { get; } = new([]);
}
