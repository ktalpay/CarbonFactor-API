using CarbonOps.Domain;

namespace CarbonOps.Domain.Tests;

public sealed class DomainSmokeTests
{
    [Fact]
    public void DomainAssemblyLoads()
    {
        Assert.Equal("CarbonOps.Domain", typeof(DomainAssemblyMarker).Assembly.GetName().Name);
    }

    [Fact]
    public void DomainDoesNotReferenceOuterLayers()
    {
        var referencedAssemblies = typeof(DomainAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        string[] forbiddenReferences =
        [
            "CarbonOps.Application",
            "CarbonOps.Contracts",
            "CarbonOps.Infrastructure",
            "CarbonOps.Api",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore"
        ];

        Assert.Empty(referencedAssemblies.Intersect(forbiddenReferences));
    }
}
