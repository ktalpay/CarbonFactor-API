using CarbonOps.Contracts;

namespace CarbonOps.Api;

internal sealed class ApiErrorException : Exception
{
    public ApiErrorException(ApiError error)
        : base(error.Message)
    {
        Error = error;
    }

    public ApiError Error { get; }
}
