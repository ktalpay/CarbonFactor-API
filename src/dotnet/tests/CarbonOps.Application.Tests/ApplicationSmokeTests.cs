using CarbonOps.Application;

namespace CarbonOps.Application.Tests;

public sealed class ApplicationSmokeTests
{
    [Fact]
    public void ApplicationAssemblyLoads()
    {
        Assert.Equal("CarbonOps.Application", typeof(ApplicationAssemblyMarker).Assembly.GetName().Name);
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        var referencedAssemblies = typeof(ApplicationAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        string[] forbiddenReferences =
        [
            "CarbonOps.Infrastructure",
            "CarbonOps.Api",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore"
        ];

        Assert.Empty(referencedAssemblies.Intersect(forbiddenReferences));
    }
}
