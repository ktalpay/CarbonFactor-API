namespace CarbonFactor.Api.Domain;

public sealed record CarbonFactorValidationResult(
    CarbonFactor? Factor,
    IReadOnlyList<CarbonFactorValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

