using CarbonOps.Application.Factors;

namespace CarbonOps.Infrastructure;

public sealed class NoOpCarbonOpsTransactionBoundary : ICarbonOpsTransactionBoundary
{
    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return operation(cancellationToken);
    }
}
