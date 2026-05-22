namespace CarbonOps.Infrastructure;

public enum PostgreSqlSchemaBootstrapMode
{
    ValidateOnly,
    CreateMissing
}

public static class PostgreSqlSchemaBootstrapModeParser
{
    public static PostgreSqlSchemaBootstrapMode Parse(string? configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return PostgreSqlSchemaBootstrapMode.ValidateOnly;
        }

        if (Enum.TryParse<PostgreSqlSchemaBootstrapMode>(
                configuredValue,
                ignoreCase: true,
                out var mode))
        {
            return mode;
        }

        throw new InvalidOperationException(
            "'Persistence:PostgreSql:BootstrapMode' must be one of: ValidateOnly, CreateMissing.");
    }
}
