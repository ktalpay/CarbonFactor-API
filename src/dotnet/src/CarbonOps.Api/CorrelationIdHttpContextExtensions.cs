namespace CarbonOps.Api;

internal static class CorrelationIdHttpContextExtensions
{
    public const string HeaderName = "X-Correlation-Id";

    internal const string ItemKey = "__CarbonOps.CorrelationId";

    public static string? GetCorrelationId(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.TryGetValue(ItemKey, out var correlationId)
            ? correlationId as string
            : null;
    }
}
