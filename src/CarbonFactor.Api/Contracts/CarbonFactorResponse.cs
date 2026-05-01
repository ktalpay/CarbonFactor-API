namespace CarbonFactor.Api.Contracts;

public sealed record CarbonFactorResponse(
    string Id,
    string Name,
    string Category,
    string Unit,
    decimal EmissionValue,
    string? Source,
    string? Region,
    int? EffectiveYear);

