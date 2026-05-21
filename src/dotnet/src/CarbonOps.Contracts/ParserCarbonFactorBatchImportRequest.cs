using System.Text.Json.Serialization;

namespace CarbonOps.Contracts;

public sealed record ParserCarbonFactorBatchImportRequest(
    [property: JsonPropertyName("contract_version")] string ContractVersion,
    [property: JsonPropertyName("batch_id")] string BatchId,
    [property: JsonPropertyName("source")] ParserSourceMetadataDto Source,
    [property: JsonPropertyName("factors")] IReadOnlyList<ParserCarbonFactorImportItem> Factors,
    [property: JsonPropertyName("parser_metadata")] ParserProvenanceMetadataDto? ParserMetadata = null);

public sealed record ParserSourceMetadataDto(
    [property: JsonPropertyName("source_system")] string SourceSystem,
    [property: JsonPropertyName("source_family")] string SourceFamily,
    [property: JsonPropertyName("source_provider")] string SourceProvider,
    [property: JsonPropertyName("publication")] string Publication,
    [property: JsonPropertyName("publication_version")] string PublicationVersion,
    [property: JsonPropertyName("published_at_utc")] DateTimeOffset? PublishedAtUtc = null);

public sealed record ParserCarbonFactorImportItem(
    [property: JsonPropertyName("external_factor_id")] string ExternalFactorId,
    [property: JsonPropertyName("source_family")] string SourceFamily,
    [property: JsonPropertyName("source_provider")] string SourceProvider,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("activity")] string Activity,
    [property: JsonPropertyName("factor_value")] decimal FactorValue,
    [property: JsonPropertyName("factor_unit")] string FactorUnit,
    [property: JsonPropertyName("region")] string? Region = null,
    [property: JsonPropertyName("year")] int? Year = null,
    [property: JsonPropertyName("factor_version")] string? FactorVersion = null,
    [property: JsonPropertyName("notes")] string? Notes = null,
    [property: JsonPropertyName("provenance")] string? Provenance = null);

public sealed record ParserProvenanceMetadataDto(
    [property: JsonPropertyName("parser_name")] string ParserName,
    [property: JsonPropertyName("parser_version")] string ParserVersion,
    [property: JsonPropertyName("parser_run_id")] string? ParserRunId = null,
    [property: JsonPropertyName("generated_at_utc")] DateTimeOffset? GeneratedAtUtc = null);
