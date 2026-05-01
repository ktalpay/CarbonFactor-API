namespace CarbonFactor.Api.Errors;

public sealed record ApiErrorResponse(
    int Status,
    string Code,
    string Title,
    string Detail,
    string TraceId,
    IReadOnlyList<ApiValidationError> Errors)
{
    public static ApiErrorResponse Create(
        int status,
        string code,
        string title,
        string detail,
        string traceId,
        IReadOnlyList<ApiValidationError>? errors = null) =>
        new(status, code, title, detail, traceId, errors ?? []);
}

