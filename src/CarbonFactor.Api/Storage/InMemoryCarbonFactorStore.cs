using System.Collections.Concurrent;
using DomainCarbonFactor = CarbonFactor.Api.Domain.CarbonFactor;

namespace CarbonFactor.Api.Storage;

public sealed class InMemoryCarbonFactorStore : ICarbonFactorStore
{
    private readonly ConcurrentDictionary<Guid, CarbonFactorRecord> records = new();

    public CarbonFactorRecord Add(DomainCarbonFactor factor)
    {
        var record = CarbonFactorRecord.FromDomain(factor);
        records[factor.Id] = record;
        return record;
    }

    public CarbonFactorRecord? Get(Guid id) =>
        records.TryGetValue(id, out var record) ? record : null;
}
