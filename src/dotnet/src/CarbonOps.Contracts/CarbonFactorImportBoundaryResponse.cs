using System.Text.Json.Serialization;

namespace CarbonOps.Contracts;

public sealed record CarbonFactorImportBoundaryResponse(
    [property: JsonPropertyName("batch_id")] string BatchId,
    [property: JsonPropertyName("audit")] CarbonFactorImportBoundaryAuditMetadata Audit,
    [property: JsonPropertyName("accepted_records")] int AcceptedRecords,
    [property: JsonPropertyName("rejected_records")] int RejectedRecords,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("validation_status")] string ValidationStatus,
    [property: JsonPropertyName("total_records")] int TotalRecords,
    [property: JsonPropertyName("warning_count")] int WarningCount,
    [property: JsonPropertyName("error_count")] int ErrorCount,
    [property: JsonPropertyName("has_warnings")] bool HasWarnings,
    [property: JsonPropertyName("has_errors")] bool HasErrors,
    [property: JsonPropertyName("persisted")] bool Persisted,
    [property: JsonPropertyName("import_execution")] string ImportExecution,
    [property: JsonPropertyName("warnings")] IReadOnlyList<CarbonFactorImportValidationMessage> Warnings,
    [property: JsonPropertyName("errors")] IReadOnlyList<CarbonFactorImportValidationMessage> Errors);


public sealed record CarbonFactorImportBoundaryAuditMetadata(
    [property: JsonPropertyName("audit_id")] string AuditId,
    [property: JsonPropertyName("batch_id")] string BatchId,
    [property: JsonPropertyName("contract_version")] string ContractVersion,
    [property: JsonPropertyName("source_system")] string SourceSystem,
    [property: JsonPropertyName("source_family")] string SourceFamily,
    [property: JsonPropertyName("source_provider")] string SourceProvider,
    [property: JsonPropertyName("publication")] string Publication,
    [property: JsonPropertyName("publication_version")] string PublicationVersion,
    [property: JsonPropertyName("parser_name")] string? ParserName,
    [property: JsonPropertyName("parser_version")] string? ParserVersion,
    [property: JsonPropertyName("parser_run_id")] string? ParserRunId,
    [property: JsonPropertyName("generated_at_utc")] DateTimeOffset? GeneratedAtUtc,
    [property: JsonPropertyName("evaluated_at_utc")] DateTimeOffset? EvaluatedAtUtc,
    [property: JsonPropertyName("tenant_id")] string TenantId,
    [property: JsonPropertyName("authentication_scheme")] string AuthenticationScheme);

public sealed record CarbonFactorImportValidationMessage(
    [property: JsonPropertyName("row_index")] int RowIndex,
    [property: JsonPropertyName("external_factor_id")] string? ExternalFactorId,
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message);
