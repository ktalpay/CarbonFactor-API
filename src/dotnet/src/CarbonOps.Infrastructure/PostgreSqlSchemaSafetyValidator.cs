using System.Text.RegularExpressions;

namespace CarbonOps.Infrastructure;

public sealed class PostgreSqlSchemaSafetyValidator
{
    private static readonly Regex DestructiveTokenRegex = new(
        @"\b(drop|truncate|delete)\b|\balter\s+table\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public void ValidateScripts(IEnumerable<PostgreSqlSchemaScript> scripts)
    {
        foreach (var script in scripts)
        {
            if (DestructiveTokenRegex.IsMatch(script.Sql))
            {
                throw new InvalidOperationException($"Schema script '{script.Name}' contains disallowed destructive SQL tokens.");
            }
        }
    }
}
