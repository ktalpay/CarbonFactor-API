using Microsoft.Extensions.Options;

namespace CarbonFactor.Api.Options;

public sealed class CarbonFactorApiOptionsValidator : IValidateOptions<CarbonFactorApiOptions>
{
    public ValidateOptionsResult Validate(string? name, CarbonFactorApiOptions options)
    {
        var failures = new List<string>();

        if (options.DefaultPageSize <= 0)
        {
            failures.Add("CarbonFactor:DefaultPageSize must be greater than zero.");
        }

        if (options.MaxPageSize <= 0)
        {
            failures.Add("CarbonFactor:MaxPageSize must be greater than zero.");
        }

        if (options.MaxPageSize > 500)
        {
            failures.Add("CarbonFactor:MaxPageSize must be less than or equal to 500.");
        }

        if (options.DefaultPageSize > options.MaxPageSize)
        {
            failures.Add("CarbonFactor:DefaultPageSize must be less than or equal to CarbonFactor:MaxPageSize.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

