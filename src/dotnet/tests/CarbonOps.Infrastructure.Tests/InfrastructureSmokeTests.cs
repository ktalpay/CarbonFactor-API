using CarbonOps.Infrastructure;

namespace CarbonOps.Infrastructure.Tests;

public sealed class InfrastructureSmokeTests
{
    [Fact]
    public void InfrastructureAssemblyLoads()
    {
        Assert.Equal("CarbonOps.Infrastructure", typeof(InfrastructureAssemblyMarker).Assembly.GetName().Name);
    }

    [Fact]
    public void InfrastructureDoesNotReferenceApi()
    {
        var referencedAssemblies = typeof(InfrastructureAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        Assert.DoesNotContain("CarbonOps.Api", referencedAssemblies);
    }
}
