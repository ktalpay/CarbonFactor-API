using CarbonOps.Application.Factors;
using CarbonOps.Contracts;

namespace CarbonOps.Api;

internal static class ApplicationResultHttpResultExtensions
{
    public static IResult ToHttpResult<T>(this ApplicationResult<T> result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }

        return result.Error!.ToHttpResult();
    }

    public static IResult ToHttpResult(this ApiError error)
    {
        return error.Code switch
        {
            "not_found" => TypedResults.NotFound(error),
            "invalid_query" => TypedResults.BadRequest(error),
            _ => TypedResults.BadRequest(error)
        };
    }
}
