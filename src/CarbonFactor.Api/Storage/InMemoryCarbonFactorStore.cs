using System.Collections.Concurrent;
using CarbonFactor.Api.Contracts;

namespace CarbonFactor.Api.Storage;

public sealed class InMemoryCarbonFactorStore : ICarbonFactorStore
{
    private readonly ConcurrentDictionary<Guid, CarbonFactorRecord> records = new();

    public CarbonFactorRecord Add(CarbonFactorCreateRequest request)
    {
        var id = Guid.NewGuid();
        var record = new CarbonFactorRecord(
            id,
            request.Name!.Trim(),
            request.Category!.Trim().ToLowerInvariant(),
            request.Unit!.Trim().ToLowerInvariant(),
            request.EmissionValue!.Value,
            NormalizeOptionalText(request.Source),
            NormalizeOptionalText(request.Region),
            request.EffectiveYear);

        records[id] = record;
        return record;
    }

    public CarbonFactorRecord? Get(Guid id) =>
        records.TryGetValue(id, out var record) ? record : null;

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

