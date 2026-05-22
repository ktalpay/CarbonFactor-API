using System.Text.Json.Nodes;

namespace CarbonOps.Contracts.Tests;

public sealed class ContractParityBaselineTests
{
    [Fact]
    public void ManifestIsPresentAndHasBaselineMetadata()
    {
        var manifest = LoadManifest();

        Assert.Equal(1, manifest["schema_version"]!.GetValue<int>());
        Assert.Equal("PT-001", manifest["task"]!.GetValue<string>());
        Assert.Equal("baseline", manifest["status"]!.GetValue<string>());
    }

    [Fact]
    public void BaselineListsCurrentPublicRouteFamilies()
    {
        var routeFamilyNames = ReadRouteFamilyNames();

        Assert.Contains("carbon_factor_read_legacy", routeFamilyNames);
        Assert.Contains("carbon_factor_read_v1", routeFamilyNames);
        Assert.Contains("carbon_factor_import_legacy", routeFamilyNames);
        Assert.Contains("carbon_factor_import_v1", routeFamilyNames);
        Assert.Contains("operational", routeFamilyNames);
        Assert.Contains("python_legacy_read", routeFamilyNames);
    }

    [Fact]
    public void BaselineIncludesLegacyAndVersionedCarbonFactorRoutes()
    {
        var routes = ReadAllRoutes();

        Assert.Contains("GET /carbon-factors", routes);
        Assert.Contains("GET /carbon-factors/search", routes);
        Assert.Contains("GET /carbon-factors/{factorId}", routes);
        Assert.Contains("POST /carbon-factors/import", routes);
        Assert.Contains("GET /v1/carbon-factors", routes);
        Assert.Contains("GET /v1/carbon-factors/search", routes);
        Assert.Contains("GET /v1/carbon-factors/{factorId}", routes);
        Assert.Contains("POST /v1/carbon-factors/import", routes);
        Assert.Contains("GET /health", routes);
        Assert.DoesNotContain("GET /v1/health", routes);
    }

    [Fact]
    public void BaselineIncludesKnownPublicResponseFieldGroups()
    {
        var manifest = LoadManifest();
        var fieldGroups = manifest["field_groups"]!.AsObject();

        AssertFieldGroupContains(fieldGroups, "factor_item", "id", "factor_value", "factor_unit", "notes");
        AssertFieldGroupContains(fieldGroups, "factor_list_response", "factors", "total");
        AssertFieldGroupContains(fieldGroups, "factor_search_response", "factors", "total");
        AssertFieldGroupContains(fieldGroups, "factor_detail_response", "factor");
        AssertFieldGroupContains(
            fieldGroups,
            "import_accepted_response",
            "batch_id",
            "audit",
            "validation_status",
            "persisted",
            "import_execution");
        AssertFieldGroupContains(
            fieldGroups,
            "import_audit_metadata",
            "tenant_id",
            "authentication_scheme");
        AssertFieldGroupContains(fieldGroups, "error_envelope", "code", "message", "details");
        AssertFieldGroupContains(fieldGroups, "version_response", "name", "version");

        var invariants = manifest["invariants"]!.AsObject();
        Assert.False(invariants["import_persisted"]!.GetValue<bool>());
        Assert.Equal("not_started", invariants["import_execution"]!.GetValue<string>());
        Assert.Equal("api_key", invariants["authentication_scheme"]!.GetValue<string>());
    }

    [Fact]
    public void BaselineIncludesErrorEnvelopeCategories()
    {
        var categories = LoadManifest()["error_envelopes"]!
            .AsArray()
            .Select(error => error!["category"]!.GetValue<string>())
            .ToArray();

        Assert.Contains("unauthorized", categories);
        Assert.Contains("invalid_query", categories);
        Assert.Contains("not_found", categories);
        Assert.Contains("rate_limited", categories);
    }

    [Fact]
    public void BaselineDocumentsPt001NonGoals()
    {
        var nonGoals = LoadManifest()["non_goals"]!
            .AsArray()
            .Select(nonGoal => nonGoal!.GetValue<string>())
            .ToArray();

        Assert.Contains("no generated client in PT-001", nonGoals);
        Assert.Contains("no OpenAPI contract drift check in PT-001", nonGoals);
        Assert.Contains("no response fixture byte-for-byte comparison in PT-001", nonGoals);
        Assert.Contains("no .NET runtime behavior changes in PT-001", nonGoals);
    }

    [Fact]
    public void BaselineRecordsPythonSideDiscovery()
    {
        var pythonSide = LoadManifest()["python_side"]!.AsObject();
        var pythonRoutes = pythonSide["current_routes"]!
            .AsArray()
            .Select(route => route!.GetValue<string>())
            .ToArray();

        Assert.Equal("src/python", pythonSide["package_root"]!.GetValue<string>());
        Assert.Contains("GET /factors", pythonRoutes);
        Assert.Contains("GET /factors/{factor_id}", pythonRoutes);
        Assert.Contains("legacy read-only contract foundation", pythonSide["status"]!.GetValue<string>());
    }

    private static string[] ReadRouteFamilyNames()
    {
        return LoadManifest()["route_families"]!
            .AsArray()
            .Select(family => family!["name"]!.GetValue<string>())
            .ToArray();
    }

    private static string[] ReadAllRoutes()
    {
        return LoadManifest()["route_families"]!
            .AsArray()
            .SelectMany(family => family!["routes"]!.AsArray())
            .Select(route => route!.GetValue<string>())
            .ToArray();
    }

    private static void AssertFieldGroupContains(JsonObject fieldGroups, string fieldGroupName, params string[] expectedFields)
    {
        var fields = fieldGroups[fieldGroupName]!
            .AsArray()
            .Select(field => field!.GetValue<string>())
            .ToArray();

        foreach (var expectedField in expectedFields)
        {
            Assert.Contains(expectedField, fields);
        }
    }

    private static JsonObject LoadManifest()
    {
        var manifestPath = FindManifestPath();
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))?.AsObject();

        Assert.NotNull(manifest);
        return manifest!;
    }

    private static string FindManifestPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidatePath = Path.Combine(
                directory.FullName,
                "tests",
                "contract-parity",
                "contract-parity-baseline.json");

            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        throw new FileNotFoundException("Could not find tests/contract-parity/contract-parity-baseline.json.");
    }
}
