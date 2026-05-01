namespace CarbonFactor.Api.Domain;

public sealed class CarbonFactorValidator
{
    public const decimal MaximumEmissionValue = 1_000_000_000m;
    public const int MinimumEffectiveYear = 1900;
    public const int MaximumEffectiveYear = 2100;

    private readonly CarbonFactorNormalizer normalizer;

    public CarbonFactorValidator(CarbonFactorNormalizer normalizer)
    {
        this.normalizer = normalizer;
    }

    public CarbonFactorValidationResult Create(Guid id, CarbonFactorInput input)
    {
        var errors = new List<CarbonFactorValidationError>();
        var normalizedName = normalizer.NormalizeName(input.Name);
        var normalizedCategory = normalizer.NormalizeCategory(input.Category);
        var normalizedUnit = normalizer.NormalizeUnit(input.Unit);

        if (id == Guid.Empty)
        {
            errors.Add(new CarbonFactorValidationError("id", "required", "Identifier is required."));
        }

        if (normalizedName is null)
        {
            errors.Add(new CarbonFactorValidationError("name", "required", "Name is required."));
        }

        if (normalizedCategory is null)
        {
            errors.Add(new CarbonFactorValidationError("category", "required", "Category is required."));
        }
        else if (!CarbonFactorNormalizer.SupportedCategories.Contains(normalizedCategory))
        {
            errors.Add(new CarbonFactorValidationError("category", "unsupported_category", "Unsupported carbon factor category."));
        }

        if (normalizedUnit is null)
        {
            errors.Add(new CarbonFactorValidationError("unit", "required", "Unit is required."));
        }
        else if (!CarbonFactorNormalizer.SupportedUnits.Contains(normalizedUnit))
        {
            errors.Add(new CarbonFactorValidationError("unit", "unsupported_unit", "Unsupported emission factor unit."));
        }

        if (input.EmissionValue is null)
        {
            errors.Add(new CarbonFactorValidationError("emissionValue", "required", "Emission value is required."));
        }
        else if (input.EmissionValue <= 0)
        {
            errors.Add(new CarbonFactorValidationError("emissionValue", "invalid_range", "Emission value must be greater than zero."));
        }
        else if (input.EmissionValue > MaximumEmissionValue)
        {
            errors.Add(new CarbonFactorValidationError("emissionValue", "invalid_range", $"Emission value must be less than or equal to {MaximumEmissionValue}."));
        }

        if (input.EffectiveYear is < MinimumEffectiveYear or > MaximumEffectiveYear)
        {
            errors.Add(new CarbonFactorValidationError("effectiveYear", "invalid_range", $"Effective year must be between {MinimumEffectiveYear} and {MaximumEffectiveYear}."));
        }

        if (errors.Count > 0)
        {
            return new CarbonFactorValidationResult(null, errors);
        }

        var factor = new CarbonFactor(
            id,
            normalizedName!,
            normalizedCategory!,
            normalizedUnit!,
            input.EmissionValue!.Value,
            normalizer.NormalizeOptionalText(input.Source),
            normalizer.NormalizeOptionalText(input.Region),
            input.EffectiveYear);

        return new CarbonFactorValidationResult(factor, []);
    }
}

