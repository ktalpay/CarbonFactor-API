using CarbonOps.Contracts;

namespace CarbonOps.Application.Factors;

public sealed record ApplicationResult<T>
{
    private ApplicationResult(T? value, ApiError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public ApiError? Error { get; }

    public bool IsSuccess => Error is null;

    public static ApplicationResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ApplicationResult<T>(value, null);
    }

    public static ApplicationResult<T> Failure(ApiError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ApplicationResult<T>(default, error);
    }
}
