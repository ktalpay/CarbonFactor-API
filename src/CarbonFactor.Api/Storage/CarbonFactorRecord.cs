namespace CarbonFactor.Api.Storage;

public sealed record CarbonFactorRecord(
    Guid Id,
    string Name,
    string Category,
    string Unit,
    decimal EmissionValue,
    string? Source,
    string? Region,
    int? EffectiveYear);

