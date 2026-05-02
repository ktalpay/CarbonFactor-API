using CarbonFactor.Api.Errors;

namespace CarbonFactor.Api.Contracts;

public sealed record CarbonFactorIngestionItemResult(
    int Index,
    string Status,
    string? Id,
    IReadOnlyList<ApiValidationError> Errors);

