namespace CarbonOps.Api;

internal sealed class CarbonOpsRateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public CarbonOpsRateLimitingPolicyOptions Import { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public CarbonOpsRateLimitingPolicyOptions Read { get; set; } = new()
    {
        PermitLimit = 60,
        WindowSeconds = 60,
        QueueLimit = 0
    };
}

internal sealed class CarbonOpsRateLimitingPolicyOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public int QueueLimit { get; set; }
}

internal sealed record ResolvedRateLimitingPolicyOptions(
    int PermitLimit,
    int WindowSeconds,
    int QueueLimit);

internal static class CarbonOpsRateLimitingPolicyNames
{
    public const string Import = "carbonops-import";
    public const string Read = "carbonops-read";
}
