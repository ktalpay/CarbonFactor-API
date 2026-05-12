namespace CarbonOps.Api.Tests;

public sealed class ApiSmokeTests
{
    [Fact]
    public void ApiAssemblyLoads()
    {
        Assert.Equal("CarbonOps.Api", typeof(Program).Assembly.GetName().Name);
    }
}
