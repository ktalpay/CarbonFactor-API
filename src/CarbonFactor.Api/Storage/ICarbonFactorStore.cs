using CarbonFactor.Api.Contracts;

namespace CarbonFactor.Api.Storage;

public interface ICarbonFactorStore
{
    CarbonFactorRecord Add(CarbonFactorCreateRequest request);

    CarbonFactorRecord? Get(Guid id);
}

