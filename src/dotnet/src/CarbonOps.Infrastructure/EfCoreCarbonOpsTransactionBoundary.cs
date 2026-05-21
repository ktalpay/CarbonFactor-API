using CarbonOps.Application.Factors;
using Microsoft.EntityFrameworkCore.Storage;

namespace CarbonOps.Infrastructure;

public sealed class EfCoreCarbonOpsTransactionBoundary(CarbonOpsDbContext dbContext) : ICarbonOpsTransactionBoundary
{
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
