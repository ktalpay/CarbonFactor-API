using CarbonOps.Contracts;

namespace CarbonOps.Api;

internal sealed class ApiErrorMappingMiddleware
{
    private readonly RequestDelegate next;

    public ApiErrorMappingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiErrorException exception) when (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = MapStatusCode(exception.Error);
            await context.Response.WriteAsJsonAsync(exception.Error);
        }
    }

    private static int MapStatusCode(ApiError error)
    {
        return error.Code switch
        {
            "unauthorized" => StatusCodes.Status401Unauthorized,
            "not_found" => StatusCodes.Status404NotFound,
            "invalid_query" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
    }
}
