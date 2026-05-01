using DomainCarbonFactor = CarbonFactor.Api.Domain.CarbonFactor;

namespace CarbonFactor.Api.Storage;

public interface ICarbonFactorStore
{
    CarbonFactorRecord Add(DomainCarbonFactor factor);

    CarbonFactorRecord? Get(Guid id);

    IReadOnlyList<CarbonFactorRecord> List();
}
