using System.Globalization;
using System.Threading.RateLimiting;
using CarbonOps.Contracts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace CarbonOps.Api;

internal static class CarbonOpsRateLimitingServiceCollectionExtensions
{
    private const string LoggerCategory = "CarbonOps.Api.RateLimiting";
    private const string RateLimitReasonCode = "rate_limit_exceeded";
    private const string FallbackPartitionKey = "carbonops-fixed-partition";

    private static readonly ResolvedRateLimitingPolicyOptions DefaultImportPolicy = new(
        PermitLimit: 10,
        WindowSeconds: 60,
        QueueLimit: 0);

    private static readonly ResolvedRateLimitingPolicyOptions DefaultReadPolicy = new(
        PermitLimit: 60,
        WindowSeconds: 60,
        QueueLimit: 0);

    public static IServiceCollection AddCarbonOpsRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<CarbonOpsRateLimitingOptions>()
            .Bind(configuration.GetSection(CarbonOpsRateLimitingOptions.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                CarbonOpsRateLimitingPolicyNames.Import,
                httpContext => CreateFixedWindowPartition(
                    httpContext,
                    ResolvePolicyOptions(
                        ResolveConfiguredOptions(httpContext).Import,
                        DefaultImportPolicy)));
            options.AddPolicy(
                CarbonOpsRateLimitingPolicyNames.Read,
                httpContext => CreateFixedWindowPartition(
                    httpContext,
                    ResolvePolicyOptions(
                        ResolveConfiguredOptions(httpContext).Read,
                        DefaultReadPolicy)));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                var endpoint = ResolveEndpoint(httpContext);
                var policyName = ResolvePolicyName(endpoint);
                var retryAfter = ResolveRetryAfter(
                    context.Lease,
                    ResolveConfiguredOptions(httpContext),
                    policyName);

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.Headers["Retry-After"] = retryAfter.TotalSeconds.ToString("0", CultureInfo.InvariantCulture);

                LogRateLimitRejection(httpContext, endpoint, policyName);
                await WriteRateLimitRejectedAuditEventAsync(httpContext, endpoint, cancellationToken);
                await httpContext.Response.WriteAsJsonAsync(
                    CreateRateLimitError(),
                    cancellationToken);
            };
        });

        return services;
    }

    private static CarbonOpsRateLimitingOptions ResolveConfiguredOptions(HttpContext httpContext)
    {
        return httpContext.RequestServices
            .GetRequiredService<IOptions<CarbonOpsRateLimitingOptions>>()
            .Value;
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext httpContext,
        ResolvedRateLimitingPolicyOptions policyOptions)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            ResolvePartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = policyOptions.PermitLimit,
                QueueLimit = policyOptions.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = TimeSpan.FromSeconds(policyOptions.WindowSeconds)
            });
    }

    private static string ResolvePartitionKey(HttpContext httpContext)
    {
        var remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(remoteIpAddress)
            ? FallbackPartitionKey
            : remoteIpAddress;
    }

    private static ResolvedRateLimitingPolicyOptions ResolvePolicyOptions(
        CarbonOpsRateLimitingPolicyOptions? configuredOptions,
        ResolvedRateLimitingPolicyOptions defaultOptions)
    {
        return new ResolvedRateLimitingPolicyOptions(
            PermitLimit: configuredOptions?.PermitLimit > 0
                ? configuredOptions.PermitLimit
                : defaultOptions.PermitLimit,
            WindowSeconds: configuredOptions?.WindowSeconds > 0
                ? configuredOptions.WindowSeconds
                : defaultOptions.WindowSeconds,
            QueueLimit: configuredOptions?.QueueLimit >= 0
                ? configuredOptions.QueueLimit
                : defaultOptions.QueueLimit);
    }

    private static TimeSpan ResolveRetryAfter(
        RateLimitLease lease,
        CarbonOpsRateLimitingOptions configuredOptions,
        string policyName)
    {
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            return retryAfter;
        }

        var policyOptions = string.Equals(policyName, CarbonOpsRateLimitingPolicyNames.Import, StringComparison.Ordinal)
            ? ResolvePolicyOptions(configuredOptions.Import, DefaultImportPolicy)
            : ResolvePolicyOptions(configuredOptions.Read, DefaultReadPolicy);

        return TimeSpan.FromSeconds(policyOptions.WindowSeconds);
    }

    private static string ResolveEndpoint(HttpContext httpContext)
    {
        if (httpContext.GetEndpoint() is RouteEndpoint routeEndpoint
            && !string.IsNullOrWhiteSpace(routeEndpoint.RoutePattern.RawText))
        {
            return NormalizeEndpoint(routeEndpoint.RoutePattern.RawText);
        }

        return NormalizeEndpoint(httpContext.Request.Path.Value ?? "unknown");
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (string.Equals(endpoint, "/", StringComparison.Ordinal)
            || string.Equals(endpoint, "unknown", StringComparison.Ordinal))
        {
            return endpoint;
        }

        return endpoint.TrimEnd('/');
    }

    private static string ResolvePolicyName(string endpoint)
    {
        return endpoint.EndsWith("/carbon-factors/import", StringComparison.Ordinal)
            ? CarbonOpsRateLimitingPolicyNames.Import
            : CarbonOpsRateLimitingPolicyNames.Read;
    }

    private static void LogRateLimitRejection(
        HttpContext httpContext,
        string endpoint,
        string policyName)
    {
        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(LoggerCategory);

        logger.LogWarning(
            "CarbonOps rate limit rejected {endpoint} {rate_limit_policy} {reason_code}",
            endpoint,
            policyName,
            RateLimitReasonCode);
    }

    private static async ValueTask WriteRateLimitRejectedAuditEventAsync(
        HttpContext httpContext,
        string endpoint,
        CancellationToken cancellationToken)
    {
        var auditEventSink = httpContext.RequestServices.GetRequiredService<IAuditEventSink>();
        await auditEventSink.WriteAsync(
            new AuditEvent(
                EventId: Guid.NewGuid().ToString(),
                EventType: AuditEventTypes.RateLimitRejected,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Severity: AuditEventSeverity.Warning,
                Endpoint: endpoint,
                CorrelationId: httpContext.GetCorrelationId(),
                Outcome: AuditEventOutcomes.Failure,
                ReasonCode: RateLimitReasonCode),
            cancellationToken);
    }

    private static ApiError CreateRateLimitError()
    {
        return new ApiError(
            "rate_limited",
            "Too many requests",
            new Dictionary<string, object> { ["reason"] = "rate limit exceeded" });
    }
}
