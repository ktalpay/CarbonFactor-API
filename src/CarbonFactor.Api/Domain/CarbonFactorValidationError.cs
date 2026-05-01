namespace CarbonFactor.Api.Domain;

public sealed record CarbonFactorValidationError(
    string Field,
    string Code,
    string Message);

