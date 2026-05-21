namespace CarbonOps.Application.Factors;

public interface ICarbonOpsTransactionBoundary
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
