using System.Text.Json.Serialization;

namespace CarbonOps.Contracts;

public sealed record CarbonFactorImportBoundaryResponse(
    [property: JsonPropertyName("batch_id")] string BatchId,
    [property: JsonPropertyName("accepted_records")] int AcceptedRecords,
    [property: JsonPropertyName("rejected_records")] int RejectedRecords,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("persisted")] bool Persisted,
    [property: JsonPropertyName("import_execution")] string ImportExecution,
    [property: JsonPropertyName("warnings")] IReadOnlyList<CarbonFactorImportValidationMessage> Warnings,
    [property: JsonPropertyName("errors")] IReadOnlyList<CarbonFactorImportValidationMessage> Errors);

public sealed record CarbonFactorImportValidationMessage(
    [property: JsonPropertyName("row_index")] int RowIndex,
    [property: JsonPropertyName("external_factor_id")] string? ExternalFactorId,
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message);
