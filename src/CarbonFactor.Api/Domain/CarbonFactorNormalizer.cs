namespace CarbonFactor.Api.Domain;

public sealed class CarbonFactorNormalizer
{
    public static readonly IReadOnlySet<string> SupportedCategories = new HashSet<string>(StringComparer.Ordinal)
    {
        "agriculture",
        "energy",
        "materials",
        "other",
        "transport",
        "waste",
        "water"
    };

    public static readonly IReadOnlySet<string> SupportedUnits = new HashSet<string>(StringComparer.Ordinal)
    {
        "g_co2e",
        "kg_co2e",
        "kg_co2e_per_km",
        "kg_co2e_per_kwh",
        "kg_co2e_per_liter",
        "kg_co2e_per_unit",
        "t_co2e"
    };

    private static readonly IReadOnlyDictionary<string, string> CategoryAliases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["agri"] = "agriculture",
        ["agriculture"] = "agriculture",
        ["electricity"] = "energy",
        ["energy"] = "energy",
        ["material"] = "materials",
        ["materials"] = "materials",
        ["other"] = "other",
        ["transport"] = "transport",
        ["transportation"] = "transport",
        ["waste"] = "waste",
        ["water"] = "water"
    };

    private static readonly IReadOnlyDictionary<string, string> UnitAliases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["g_co2e"] = "g_co2e",
        ["gco2e"] = "g_co2e",
        ["kg_co2e"] = "kg_co2e",
        ["kgco2e"] = "kg_co2e",
        ["kg_co2e_km"] = "kg_co2e_per_km",
        ["kg_co2e_liter"] = "kg_co2e_per_liter",
        ["kg_co2e_kwh"] = "kg_co2e_per_kwh",
        ["kg_co2e_unit"] = "kg_co2e_per_unit",
        ["kgco2e_km"] = "kg_co2e_per_km",
        ["kgco2e_liter"] = "kg_co2e_per_liter",
        ["kgco2e_kwh"] = "kg_co2e_per_kwh",
        ["kgco2e_unit"] = "kg_co2e_per_unit",
        ["t_co2e"] = "t_co2e",
        ["tco2e"] = "t_co2e",
        ["tonne_co2e"] = "t_co2e",
        ["tonnes_co2e"] = "t_co2e"
    };

    public string? NormalizeCategory(string? value)
    {
        var key = NormalizeToken(value);
        return key is not null && CategoryAliases.TryGetValue(key, out var normalized)
            ? normalized
            : key;
    }

    public string? NormalizeUnit(string? value)
    {
        var key = NormalizeToken(value);
        if (key is null)
        {
            return null;
        }

        key = key
            .Replace("_per_", "_", StringComparison.Ordinal)
            .Replace("_/_", "_", StringComparison.Ordinal);

        return UnitAliases.TryGetValue(key, out var normalized)
            ? normalized
            : key;
    }

    public string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public string? NormalizeName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : CollapseWhitespace(value.Trim());

    private static string? NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return CollapseWhitespace(value.Trim())
            .ToLowerInvariant()
            .Replace("co2e", "co2e", StringComparison.Ordinal)
            .Replace(" / ", "_", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal);
    }

    private static string CollapseWhitespace(string value) =>
        string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

