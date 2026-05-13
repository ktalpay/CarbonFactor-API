using CarbonOps.Domain;

namespace CarbonOps.Application.Factors;

public interface ICarbonFactorRepository
{
    IReadOnlyCollection<CarbonFactor> ListCarbonFactors();

    CarbonFactor? GetCarbonFactorById(string factorId);
}
