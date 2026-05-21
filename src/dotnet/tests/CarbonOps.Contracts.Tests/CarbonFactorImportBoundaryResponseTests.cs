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
            new CarbonFactorImportBoundaryAuditMetadata("aid","batch-001","1.0","parser","electricity","synthetic","pub","v1","name","1.2","run-1",null,null,"tenant-dev-001","api_key"),
            10,
            2,
            "accepted_with_validation_errors",
            "accepted_with_validation_errors",
            12,
            1,
            2,
            true,
            true,
            false,
            "not_started",
            [new CarbonFactorImportValidationMessage(0, "ext-1", "source", "source_mismatch", "warning")],
            [new CarbonFactorImportValidationMessage(1, "ext-2", "factor_unit", "required", "factor_unit is required")]);

        var payload = JsonSerializer.SerializeToElement(response);

        Assert.Equal("batch-001", payload.GetProperty("batch_id").GetString());
        Assert.Equal("aid", payload.GetProperty("audit").GetProperty("audit_id").GetString());
        Assert.Equal("tenant-dev-001", payload.GetProperty("audit").GetProperty("tenant_id").GetString());
        Assert.Equal("api_key", payload.GetProperty("audit").GetProperty("authentication_scheme").GetString());
        Assert.Equal(10, payload.GetProperty("accepted_records").GetInt32());
        Assert.Equal(2, payload.GetProperty("rejected_records").GetInt32());
        Assert.Equal("accepted_with_validation_errors", payload.GetProperty("validation_status").GetString());
        Assert.Equal(12, payload.GetProperty("total_records").GetInt32());
        Assert.Equal(1, payload.GetProperty("warning_count").GetInt32());
        Assert.Equal(2, payload.GetProperty("error_count").GetInt32());
        Assert.True(payload.GetProperty("has_warnings").GetBoolean());
        Assert.True(payload.GetProperty("has_errors").GetBoolean());
        Assert.False(payload.GetProperty("persisted").GetBoolean());
        Assert.Equal("not_started", payload.GetProperty("import_execution").GetString());
        Assert.Equal("row_index", payload.GetProperty("errors")[0].EnumerateObject().First().Name);
    }
}
