using CarbonFactor.Api.Options;
using Microsoft.Extensions.Options;

namespace CarbonFactor.Api.Tests;

public sealed class CarbonFactorApiOptionsTests
{
    private readonly CarbonFactorApiOptionsValidator validator = new();

    [Fact]
    public void DefaultOptions_AreValid()
    {
        var result = validator.Validate(null, new CarbonFactorApiOptions());

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void CustomValidOptions_AreValid()
    {
        var result = validator.Validate(
            null,
            new CarbonFactorApiOptions
            {
                DefaultPageSize = 10,
                MaxPageSize = 50,
                EnableSwaggerInProduction = false
            });

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(25, 0)]
    [InlineData(101, 100)]
    [InlineData(25, 501)]
    public void InvalidNumericLimits_ReturnValidationFailures(int defaultPageSize, int maxPageSize)
    {
        var result = validator.Validate(
            null,
            new CarbonFactorApiOptions
            {
                DefaultPageSize = defaultPageSize,
                MaxPageSize = maxPageSize
            });

        Assert.True(result.Failed);
        Assert.NotEmpty(result.Failures);
    }
}

