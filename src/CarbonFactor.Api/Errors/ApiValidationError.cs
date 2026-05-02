namespace CarbonFactor.Api.Errors;

public sealed record ApiValidationError(
    string Field,
    string Code,
    string Message);

