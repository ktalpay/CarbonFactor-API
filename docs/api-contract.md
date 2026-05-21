# CarbonOps-API Contract (Pre-Alpha)

## Current Status

- Status: **pre-alpha**
- HTTP server: **thin FastAPI adapter for local testing only**
- Contract style: deterministic in-memory models and service behavior
- Transport boundary: deterministic local envelope + status mapping layer (no framework)

## DTO Models

CarbonFactor remains the domain concept for a carbon factor record.

`FactorDto` fields:
- `id`
- `source`
- `category`
- `activity`
- `factor_value`
- `factor_unit`
- `region` (optional)
- `year` (optional)
- `notes` (optional)

`FactorQuery` filter fields:
- `category`
- `activity`
- `region`
- `year`

## Transport Contract Layer

Envelope shape:
- `status`
- `data`
- `error`

Error shape:
- `code`
- `message`
- `details`

Status mappings:
- `200` success
- `400` invalid query / unsupported filter
- `404` not found
- `422` validation-style error (reserved)

## Behavior

- List and search factors with deterministic sorting by `id`, with explicit
  tie-breakers applied before pagination.
- Get factor by exact `id`.
- Search by supported filters (`category`, `activity`, `region`, `year`).
- Return structured error objects for not-found and invalid queries.
- Serialize responses through deterministic transport helpers.

## Local Handler Entry Points

- `handle_list_factors(query: dict)`
- `handle_get_factor(factor_id: str)`

These are local functions only and are intended to be adapted into a future HTTP framework.

## Limitations

- No production HTTP transport server.
- No persistence/database.
- Uses only synthetic sample data.
- Not production-ready and not a complete emissions data standard implementation.
- No CarbonOps-Parser runtime integration.
- No CarbonOps-Web runtime integration.

## HTTP adapter mapping (local)

`GET /factors` and `GET /factors/{factor_id}` are exposed through a thin FastAPI adapter that reuses the transport envelope contract.


## HTTP query contract

`GET /factors` accepts `category`, `activity`, `region`, and `year`. Unsupported query keys are normalized into deterministic `invalid_query` envelope errors with `unsupported_query_keys` details. OpenAPI contract inspection is test-covered, but no generated OpenAPI artifact is published in-repo.


See also evidence docs: `docs/evidence/index.md`, `docs/evidence/api-architecture-evidence.md`.

## Parser output ingestion contract (ING-001)

A versioned parser-output ingestion request contract is now defined in `CarbonOps.Contracts` for planned write-side ingestion, with no runtime ingestion behavior enabled in this task.

Contract types:
- `ParserCarbonFactorBatchImportRequest`
- `ParserSourceMetadataDto`
- `ParserCarbonFactorImportItem`
- `ParserProvenanceMetadataDto`

Notes:
- This is contract-only scope for ING-001.
- No ingestion endpoint exists yet; endpoint boundary is planned for ING-002.
- No DB writes are added by this task.
- SEC-001 will introduce API key authentication baseline.
- Data correctness/provenance enforcement remains a future controlled ingestion validation concern.


## Carbon factor import boundary (ING-002)

- `POST /carbon-factors/import` accepts `ParserCarbonFactorBatchImportRequest` from `CarbonOps.Contracts`.
- The endpoint validates boundary shape and returns deterministic responses.
- Valid boundary requests return `202 Accepted` and `persisted=false` with `import_execution="not_started"`.
- Invalid boundary requests return `invalid_query` errors.
- This boundary does **not** persist data yet; no database write/import execution occurs in ING-002.
- SEC-001 will add API key authentication.
- Future ING tasks will implement import execution and persistence.

## Carbon factor import validation model (ING-003)

- `POST /carbon-factors/import` now performs deterministic row-level validation while still remaining boundary-only (no persistence and no import execution).
- Required batch-level validation:
  - `contract_version` required and must be supported (`1.0`).
  - `batch_id` required.
  - `source` required.
  - `source.source_family` and `source.source_provider` required.
  - `factors` must contain at least one item.
- Required factor-level validation:
  - `external_factor_id`, `source_family`, `source_provider`, `category`, `activity`, `factor_unit` required.
  - `factor_value` must be `>= 0`.
  - `year` (when provided) must be in deterministic range `1900..2100`.
  - duplicate `external_factor_id` values in the same batch are rejected deterministically.
- Source mismatch (`source_family`/`source_provider` between batch source and factor item) is preserved as a deterministic **warning** (not a rejection), aligned with ING-002 behavior.

### Mixed valid/invalid row policy

- If at least one row is valid, endpoint returns `202 Accepted` with:
  - `accepted_records`
  - `rejected_records`
  - `status = "accepted_with_validation_errors"` when any rows are invalid
  - row-level `errors` and `warnings`
  - `persisted = false`
  - `import_execution = "not_started"`
- If zero valid rows remain after validation, endpoint returns `400 invalid_query` with reason `no valid factor rows remain after validation`.

### Row-level validation details (snake_case)

Each validation message now uses deterministic shape:
- `row_index`
- `external_factor_id` (nullable if missing)
- `field`
- `code`
- `message`

### Security and non-goals

- SEC-001 authentication scope is unchanged.
- No DB writes.
- No import execution/background job.
- No parser project changes.

## Source version and duplicate handling (ING-004)

- `POST /carbon-factors/import` remains boundary-only and deterministic (`persisted=false`, `import_execution="not_started"`), with no DB writes or import execution.
- Additional required batch source metadata:
  - `source.publication` is required.
  - `source.publication_version` is required.
- When `parser_metadata` is present, both fields are required:
  - `parser_metadata.parser_name`
  - `parser_metadata.parser_version`
- `published_at_utc` and `generated_at_utc` remain optional DTO fields and continue to flow via deterministic snake_case JSON contract serialization.

### Duplicate handling policy

- Duplicate `external_factor_id` in the same batch remains a row-level validation error (`field=external_factor_id`, `code=duplicate`).
- Duplicate factor identity in the same batch is now detected deterministically and rejected for the **later** row(s), using:
  - `source_provider`
  - `source_family`
  - `category`
  - `activity`
  - `region` (null/blank normalized consistently)
  - `year` (normalized consistently)
  - `factor_version` (null/blank normalized consistently)
  - `factor_unit`
- Duplicate factor identity emits row-level validation error:
  - `field=factor_identity`
  - `code=duplicate_factor_identity`
- `factor_version` participates in identity comparison when present; different non-blank versions are treated as distinct identities.

### Mixed-batch behavior (unchanged)

- At least one valid row: `202 Accepted` with accepted/rejected counts and row-level warnings/errors.
- Zero valid rows after validation: `400 invalid_query`.

### Non-goals preserved

- No persistence/import execution behavior.
- No auth in this task (SEC-001 remains separate).
- No tenant scoping in this task (SEC-002 remains separate).

## Import result reporting (ING-005)

- `POST /carbon-factors/import` now returns deterministic import result summaries while remaining boundary-only (`persisted=false`, `import_execution="not_started"`).
- Response fields are snake_case and machine-readable:
  - `total_records`
  - `accepted_records`
  - `rejected_records`
  - `warning_count`
  - `error_count`
  - `has_warnings`
  - `has_errors`
  - `status`
  - `validation_status`
  - `warnings`
  - `errors`

### Validation status policy

- `accepted`: all rows valid and no warnings.
- `accepted_with_warnings`: at least one warning and zero rejected rows.
- `accepted_with_validation_errors`: at least one valid row and at least one rejected row.
- If zero valid rows remain after validation, endpoint behavior is unchanged: `400 invalid_query`.

### Deterministic message ordering

- Validation warnings and errors are deterministic and ordered by:
  1. `row_index` ascending
  2. `field` ascending (ordinal)
  3. `code` ascending (ordinal)

### Non-goals preserved

- No data persistence.
- No import execution / background jobs.
- No auth implementation in this task (SEC-001 remains separate).
- No tenant scoping in this task (SEC-002 remains separate).

## Import audit trail (ING-006)

See also: `docs/ingestion-production-readiness.md` for ING-007 production-readiness findings and handoff risks.


- `POST /carbon-factors/import` now includes deterministic boundary-level audit metadata under `audit` while remaining boundary-only (`persisted=false`, `import_execution="not_started"`).
- Audit metadata is request-derived and not persisted to durable storage in this scope.

### Audit metadata shape (snake_case)

- `audit_id` (deterministic SHA-256 hash of canonical import boundary identity)
- `batch_id`
- `contract_version`
- `source_system`
- `source_family`
- `source_provider`
- `publication`
- `publication_version`
- `parser_name` (nullable)
- `parser_version` (nullable)
- `parser_run_id` (nullable)
- `generated_at_utc` (nullable)
- `evaluated_at_utc` (nullable; currently mirrors parser `generated_at_utc` when present)

### Determinism policy

- `audit_id` is deterministic for equivalent boundary inputs and changes when key source identity fields change (for example `batch_id` or `publication_version`).
- No random identifiers are used.
- No runtime persistence or import execution is introduced.

### Non-goals preserved

- No persistence/import execution yet.
- No durable audit storage in this task.
- No auth in this task (SEC-001 remains separate).
- No tenant scoping in this task (SEC-002 remains separate).


## API key authentication baseline (SEC-001)

- Authentication header: `X-Api-Key`.
- Protected route(s): `POST /carbon-factors/import` only.
- Current read endpoints remain public in this phase (`GET /carbon-factors`, `GET /carbon-factors/search`, `GET /carbon-factors/{factorId}`).
- API key value is configuration-driven via `Security:ApiKey:ImportEndpointKey`.
- If `Security:ApiKey:ImportEndpointKey` is missing/blank, import endpoint fails closed with `401 unauthorized`.
- Missing or invalid key returns deterministic `401` envelope (`code=unauthorized`) without echoing key material.
- `appsettings.Development.json` may contain an obvious non-production placeholder for local/testing only.
- Tenant/company scoping is not implemented in SEC-001; planned for SEC-002.
- This baseline is authentication only and does not provide tenant isolation or role/scope authorization.
