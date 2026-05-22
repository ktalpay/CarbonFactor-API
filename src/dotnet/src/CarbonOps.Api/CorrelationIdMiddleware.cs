namespace CarbonOps.Api;

internal sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<CorrelationIdMiddleware> logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers);
        context.Items[CorrelationIdHttpContextExtensions.ItemKey] = correlationId;
        context.Response.OnStarting(
            static state =>
            {
                var httpContext = (HttpContext)state;
                if (httpContext.GetCorrelationId() is { Length: > 0 } correlationId)
                {
                    httpContext.Response.Headers[CorrelationIdHttpContextExtensions.HeaderName] = correlationId;
                }

                return Task.CompletedTask;
            },
            context);

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["correlation_id"] = correlationId
        }))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(CorrelationIdHttpContextExtensions.HeaderName, out var values)
            || values.Count != 1)
        {
            return GenerateCorrelationId();
        }

        var candidate = values[0];
        return IsValidCorrelationId(candidate)
            ? candidate!
            : GenerateCorrelationId();
    }

    private static string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValidCorrelationId(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128)
        {
            return false;
        }

        foreach (var character in correlationId)
        {
            if (char.IsWhiteSpace(character) || !IsAllowedCorrelationIdCharacter(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAllowedCorrelationIdCharacter(char value)
    {
        return value is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9'
            or '-'
            or '_'
            or '.'
            or ':';
    }
}
