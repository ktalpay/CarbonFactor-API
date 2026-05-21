using System.Text.Json;
using CarbonOps.Contracts;

namespace CarbonOps.Contracts.Tests;

public sealed class CarbonFactorImportBoundaryResponseTests
{
    [Fact]
    public void SerializesWithDeterministicSnakeCaseFieldNames()
    {
        var response = new CarbonFactorImportBoundaryResponse(
            "batch-001",
            10,
            0,
            "accepted_boundary_only",
            false,
            "not_started",
            ["warning-1"],
            []);

        var payload = JsonSerializer.SerializeToElement(response);

        Assert.Equal("batch-001", payload.GetProperty("batch_id").GetString());
        Assert.Equal(10, payload.GetProperty("accepted_records").GetInt32());
        Assert.Equal(0, payload.GetProperty("rejected_records").GetInt32());
        Assert.False(payload.GetProperty("persisted").GetBoolean());
        Assert.Equal("not_started", payload.GetProperty("import_execution").GetString());
    }
}
