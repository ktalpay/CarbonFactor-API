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
- Ingestion endpoint boundary now exists at `POST /carbon-factors/import` (ING-002).
- No DB writes are added by this task.
- SEC-001 will introduce API key authentication baseline.
- Data correctness/provenance enforcement remains a future controlled ingestion validation concern.


## Carbon factor import endpoint boundary (ING-002)

- Endpoint: `POST /carbon-factors/import`
- Request contract: `ParserCarbonFactorBatchImportRequest` (`snake_case` fields).
- Boundary behavior: validates request shape and required fields, but does **not** persist records yet.
- Response includes `batch_id`, accepted/rejected counts, warnings, and explicit non-persistence flags (`persisted=false`, `import_execution=not_started`).
- Current auth status: no API key auth yet (planned in SEC-001).
- Full import execution/persistence and deeper provenance rules are deferred to future ingestion tasks.

Example request:
```json
{
  "contract_version": "1.0",
  "batch_id": "batch-001",
  "source": {"source_family": "epa", "source_provider": "us"},
  "factors": [
    {
      "external_factor_id": "ef-1",
      "source_family": "epa",
      "source_provider": "us",
      "category": "electricity",
      "activity": "grid",
      "factor_value": 0.45,
      "factor_unit": "kgCO2e/kWh"
    }
  ]
}
```

Example response:
```json
{
  "status": 202,
  "data": {
    "batch_id": "batch-001",
    "accepted_records": 1,
    "rejected_records": 0,
    "status": "accepted_for_validation_boundary",
    "persisted": false,
    "import_execution": "not_started",
    "warnings": [],
    "errors": []
  },
  "error": null
}
```
