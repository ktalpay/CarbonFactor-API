using CarbonOps.Contracts;

namespace CarbonOps.Contracts.Tests;

public sealed class ContractsSmokeTests
{
    [Fact]
    public void ContractsAssemblyLoads()
    {
        Assert.Equal("CarbonOps.Contracts", typeof(ContractsAssemblyMarker).Assembly.GetName().Name);
    }

    [Fact]
    public void ContractsDoesNotReferenceOuterRuntimeLayers()
    {
        var referencedAssemblies = typeof(ContractsAssemblyMarker)
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
