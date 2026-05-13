using CarbonOps.Contracts;
using CarbonOps.Domain;

namespace CarbonOps.Application.Factors;

internal static class CarbonFactorMapper
{
    public static FactorDto ToDto(CarbonFactor factor)
    {
        return new FactorDto(
            factor.Id,
            factor.Source,
            factor.Category,
            factor.Activity,
            factor.FactorValue,
            factor.FactorUnit,
            factor.Region,
            factor.Year,
            factor.Notes);
    }
}
