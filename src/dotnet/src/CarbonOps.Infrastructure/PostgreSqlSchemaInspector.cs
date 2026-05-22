using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CarbonOps.Infrastructure;

public sealed class PostgreSqlSchemaInspector(CarbonOpsDbContext dbContext) : IPostgreSqlSchemaInspector
{
    private const string SchemaName = "carbonops";
    private const string TableName = "carbon_factors";

    private static readonly string[] RequiredIndexes =
    [
        "ux_carbon_factors_identity",
        "ix_carbon_factors_query",
        "ix_carbon_factors_source"
    ];

    public async Task<PostgreSqlSchemaInspectionResult> InspectAsync(CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        var closeConnection = connection.State == ConnectionState.Closed;

        if (closeConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var missingObjects = new List<string>();

            if (!await ExistsAsync(
                    connection,
                    "select exists(select 1 from information_schema.schemata where schema_name = @schema_name);",
                    cancellationToken,
                    ("schema_name", SchemaName)))
            {
                missingObjects.Add("schema carbonops");
            }

            if (!await ExistsAsync(
                    connection,
                    """
                    select exists(
                        select 1
                        from information_schema.tables
                        where table_schema = @schema_name
                          and table_name = @table_name);
                    """,
                    cancellationToken,
                    ("schema_name", SchemaName),
                    ("table_name", TableName)))
            {
                missingObjects.Add("table carbonops.carbon_factors");
            }

            foreach (var indexName in RequiredIndexes)
            {
                if (!await ExistsAsync(
                        connection,
                        """
                        select exists(
                            select 1
                            from pg_indexes
                            where schemaname = @schema_name
                              and tablename = @table_name
                              and indexname = @index_name);
                        """,
                        cancellationToken,
                        ("schema_name", SchemaName),
                        ("table_name", TableName),
                        ("index_name", indexName)))
                {
                    missingObjects.Add($"index {indexName}");
                }
            }

            return new PostgreSqlSchemaInspectionResult(missingObjects);
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> ExistsAsync(
        DbConnection connection,
        string commandText,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }
}
