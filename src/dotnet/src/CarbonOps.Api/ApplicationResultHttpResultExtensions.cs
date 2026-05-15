using CarbonOps.Application.Factors;
using CarbonOps.Contracts;

namespace CarbonOps.Api;

internal static class ApplicationResultHttpResultExtensions
{
    public static T GetValueOrThrow<T>(this ApplicationResult<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new ApiErrorException(result.Error!);
    }

    public static void ThrowIfError(this ApiError? error)
    {
        if (error is null)
        {
            return;
        }

        throw new ApiErrorException(error);
    }
}
