namespace CarbonFactor.Api.Domain;

public sealed record CarbonFactor(
    Guid Id,
    string Name,
    string Category,
    string Unit,
    decimal EmissionValue,
    string? Source,
    string? Region,
    int? EffectiveYear);

