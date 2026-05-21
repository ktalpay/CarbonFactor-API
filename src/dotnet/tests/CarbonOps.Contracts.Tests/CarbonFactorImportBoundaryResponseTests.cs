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
            2,
            "accepted_with_validation_errors",
            false,
            "not_started",
            [new CarbonFactorImportValidationMessage(0, "ext-1", "source", "source_mismatch", "warning")],
            [new CarbonFactorImportValidationMessage(1, "ext-2", "factor_unit", "required", "factor_unit is required")]);

        var payload = JsonSerializer.SerializeToElement(response);

        Assert.Equal("batch-001", payload.GetProperty("batch_id").GetString());
        Assert.Equal(10, payload.GetProperty("accepted_records").GetInt32());
        Assert.Equal(2, payload.GetProperty("rejected_records").GetInt32());
        Assert.False(payload.GetProperty("persisted").GetBoolean());
        Assert.Equal("not_started", payload.GetProperty("import_execution").GetString());
        Assert.Equal("row_index", payload.GetProperty("errors")[0].EnumerateObject().First().Name);
    }
}
