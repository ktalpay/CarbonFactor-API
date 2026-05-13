namespace CarbonOps.Domain;

public sealed record CarbonFactor
{
    public CarbonFactor(
        string id,
        string source,
        string category,
        string activity,
        decimal factorValue,
        string factorUnit,
        string? region = null,
        int? year = null,
        string? notes = null)
    {
        Id = RequireText(id, nameof(id));
        Source = RequireText(source, nameof(source));
        Category = RequireText(category, nameof(category));
        Activity = RequireText(activity, nameof(activity));
        FactorValue = factorValue;
        FactorUnit = RequireText(factorUnit, nameof(factorUnit));
        Region = region;
        Year = year;
        Notes = notes;
    }

    public string Id { get; }

    public string Source { get; }

    public string Category { get; }

    public string Activity { get; }

    public decimal FactorValue { get; }

    public string FactorUnit { get; }

    public string? Region { get; }

    public int? Year { get; }

    public string? Notes { get; }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", parameterName);
        }

        return value;
    }
}
