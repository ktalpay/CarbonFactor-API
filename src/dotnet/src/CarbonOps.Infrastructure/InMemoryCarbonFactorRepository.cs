using CarbonOps.Application.Factors;
using CarbonOps.Domain;

namespace CarbonOps.Infrastructure;

public sealed class InMemoryCarbonFactorRepository : ICarbonFactorRepository
{
    private static readonly IReadOnlyList<CarbonFactor> SeededFactors = CarbonFactorReferenceDataCatalog.GetSampleCarbonFactors();
    private static readonly IReadOnlyDictionary<string, CarbonFactor> FactorsById = SeededFactors
        .ToDictionary(factor => factor.Id, StringComparer.Ordinal);

    public IReadOnlyCollection<CarbonFactor> ListCarbonFactors()
    {
        return SeededFactors;
    }

    public CarbonFactor? GetCarbonFactorById(string factorId)
    {
        return FactorsById.GetValueOrDefault(factorId);
    }

}
