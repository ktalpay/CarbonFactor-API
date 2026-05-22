using System.Data;
using Microsoft.EntityFrameworkCore;

namespace CarbonOps.Infrastructure;

public sealed class PostgreSqlSchemaExecutor(CarbonOpsDbContext dbContext) : IPostgreSqlSchemaExecutor
{
    public async Task ExecuteAsync(IReadOnlyList<PostgreSqlSchemaScript> scripts, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        var closeConnection = connection.State == ConnectionState.Closed;

        if (closeConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            foreach (var script in scripts)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = script.Sql;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}
