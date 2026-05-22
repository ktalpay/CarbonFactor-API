# Public Endpoints (DOC-001)

Date: 2026-05-22

## Purpose And Scope

This guide documents the current public HTTP endpoint surface for CarbonOps-API. It is grounded in the current `.NET` API behavior, checked-in response fixtures, and public route metadata baseline.

This is not a full generated OpenAPI document, generated client guide, architecture document, or production readiness checklist. It does not claim broad production readiness.

## Base URL And Versioning

Use this placeholder base URL in examples:

```text
https://api.example.com
```

Current preferred carbon factor route prefix:

```text
/v1
```

Legacy unversioned carbon factor routes are still supported for compatibility. New consumers should prefer `/v1`.

Operational endpoints are intentionally unversioned:

```text
/health
/health/live
/health/ready
/version
```

There is no `/v1/health` endpoint.

## Headers

### `X-Correlation-Id`

`X-Correlation-Id` is an optional request header.

Behavior:

- If supplied and valid, the API echoes the same value in the response.
- If missing, blank, invalid, too long, or duplicated, the API generates a new correlation id.
- Every response includes `X-Correlation-Id`, including success, validation failure, auth failure, and rate-limited responses.

Use this header for troubleshooting across client logs and API logs. Do not put secrets into correlation ids.

### `X-Api-Key`

`X-Api-Key` is required only for import endpoints:

```text
POST /v1/carbon-factors/import
POST /carbon-factors/import
```

Use placeholders in examples:

```text
X-Api-Key: <api_key>
```

Never paste real API keys into docs, issues, logs, screenshots, PR comments, or support tickets.

### `Retry-After`

`Retry-After` is present on `429 Too Many Requests` responses.

## Public Read Endpoints

Read endpoints are public and use the `read` rate-limit policy. Defaults in `appsettings.json` are `60` permits per `60` seconds with queue limit `0`; production deployments can override those values.

### List Carbon Factors

```text
GET /v1/carbon-factors
```

Legacy equivalent:

```text
GET /carbon-factors
```

Purpose: return the current deterministic carbon factor sample list.

Auth: public.

Rate limit policy: `read`.

Supported query parameters:

- `offset`
- `limit`

Example request:

```bash
curl -i \
  -H "X-Correlation-Id: docs-list-001" \
  https://api.example.com/v1/carbon-factors
```

Example response excerpt, based on `tests/contract-fixtures/http/carbon-factors-list.v1.json`:

```json
{
  "factors": [
    {
      "id": "f-001",
      "source": "synthetic",
      "category": "electricity",
      "activity": "grid electricity",
      "factor_value": 0.42,
      "factor_unit": "kgCO2e/kWh",
      "region": "US-WEST",
      "year": 2024,
      "notes": "grid sample"
    }
  ],
  "total": 3
}
```

### Search Carbon Factors

```text
GET /v1/carbon-factors/search
```

Legacy equivalent:

```text
GET /carbon-factors/search
```

Purpose: filter carbon factors by supported query parameters.

Auth: public.

Rate limit policy: `read`.

Supported query parameters:

- `category`
- `activity`
- `region`
- `year`
- `offset`
- `limit`

Example request:

```bash
curl -i \
  "https://api.example.com/v1/carbon-factors/search?category=electricity&activity=grid%20electricity&region=US-WEST&year=2024"
```

Example response, based on `tests/contract-fixtures/http/carbon-factors-search-electricity.v1.json`:

```json
{
  "factors": [
    {
      "id": "f-001",
      "source": "synthetic",
      "category": "electricity",
      "activity": "grid electricity",
      "factor_value": 0.42,
      "factor_unit": "kgCO2e/kWh",
      "region": "US-WEST",
      "year": 2024,
      "notes": "grid sample"
    }
  ],
  "total": 1
}
```

Validation notes:

- Unsupported query keys return `400 invalid_query`.
- Each supported query parameter must be provided at most once.
- Provided values must not be blank.
- `year`, `offset`, and `limit` must be integers.
- `year` must be positive when supplied.
- `offset` must be zero or positive.
- `limit` must be positive.

### Get Carbon Factor By Id

```text
GET /v1/carbon-factors/{factorId}
```

Legacy equivalent:

```text
GET /carbon-factors/{factorId}
```

Purpose: return a single carbon factor by exact id.

Auth: public.

Rate limit policy: `read`.

Example request:

```bash
curl -i https://api.example.com/v1/carbon-factors/f-001
```

Example response, based on `tests/contract-fixtures/http/carbon-factor-f001.v1.json`:

```json
{
  "factor": {
    "id": "f-001",
    "source": "synthetic",
    "category": "electricity",
    "activity": "grid electricity",
    "factor_value": 0.42,
    "factor_unit": "kgCO2e/kWh",
    "region": "US-WEST",
    "year": 2024,
    "notes": "grid sample"
  }
}
```

Validation notes:

- `factorId` is required as a path segment.
- `factorId` must not contain whitespace.
- Unknown ids return `404 not_found`.

## Protected Import Endpoint

```text
POST /v1/carbon-factors/import
```

Legacy equivalent:

```text
POST /carbon-factors/import
```

Purpose: validate and accept a parser carbon factor batch at the API boundary.

Auth: `X-Api-Key` required.

Rate limit policy: `import`.

Default rate limit in `appsettings.json`: `10` permits per `60` seconds with queue limit `0`; production deployments can override this value.

Request body shape, representative minimum:

```json
{
  "contract_version": "1.0",
  "batch_id": "batch-1",
  "source": {
    "source_system": "parser",
    "source_family": "electricity",
    "source_provider": "synthetic",
    "publication": "pub",
    "publication_version": "v1"
  },
  "factors": [
    {
      "external_factor_id": "ext-1",
      "source_family": "electricity",
      "source_provider": "synthetic",
      "category": "electricity",
      "activity": "grid",
      "factor_value": 0.1,
      "factor_unit": "kg"
    }
  ]
}
```

Example request:

```bash
curl -i -X POST https://api.example.com/v1/carbon-factors/import \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: <api_key>" \
  -H "X-Correlation-Id: docs-import-001" \
  -d '{
    "contract_version": "1.0",
    "batch_id": "batch-1",
    "source": {
      "source_system": "parser",
      "source_family": "electricity",
      "source_provider": "synthetic",
      "publication": "pub",
      "publication_version": "v1"
    },
    "factors": [
      {
        "external_factor_id": "ext-1",
        "source_family": "electricity",
        "source_provider": "synthetic",
        "category": "electricity",
        "activity": "grid",
        "factor_value": 0.1,
        "factor_unit": "kg"
      }
    ]
  }'
```

Accepted response excerpt, based on `tests/contract-fixtures/http/import-accepted.v1.json`:

```json
{
  "batch_id": "batch-1",
  "audit": {
    "batch_id": "batch-1",
    "contract_version": "1.0",
    "source_system": "parser",
    "source_family": "electricity",
    "source_provider": "synthetic",
    "publication": "pub",
    "publication_version": "v1",
    "tenant_id": "<configured_tenant_id>",
    "authentication_scheme": "api_key"
  },
  "accepted_records": 1,
  "rejected_records": 0,
  "status": "accepted",
  "validation_status": "accepted",
  "total_records": 1,
  "warning_count": 0,
  "error_count": 0,
  "persisted": false,
  "import_execution": "not_started",
  "warnings": [],
  "errors": []
}
```

Import invariants:

- Valid boundary requests return `202 Accepted`.
- `persisted=false`.
- `import_execution="not_started"`.
- `audit.authentication_scheme="api_key"`.
- `audit.tenant_id` comes from runtime configuration.
- API keys, configured hashes, revoked hashes, previous hashes, and configured scopes are not exposed in public responses.

Missing API key behavior:

```json
{
  "code": "unauthorized",
  "message": "Unauthorized",
  "details": {
    "reason": "missing API key"
  }
}
```

## Error Responses

Error responses use the shared envelope:

- `code`
- `message`
- `details`

### `401 unauthorized`

Based on `tests/contract-fixtures/http/import-unauthorized-missing-api-key.v1.json`:

```json
{
  "code": "unauthorized",
  "message": "Unauthorized",
  "details": {
    "reason": "missing API key"
  }
}
```

### `400 invalid_query`

Based on `tests/contract-fixtures/http/search-invalid-year.v1.json`:

```json
{
  "code": "invalid_query",
  "message": "Invalid query",
  "details": {
    "reason": "year must be positive"
  }
}
```

### `404 not_found`

Based on `tests/contract-fixtures/http/factor-not-found.v1.json`:

```json
{
  "code": "not_found",
  "message": "factor not found",
  "details": {
    "id": "missing-factor"
  }
}
```

### `429 rate_limited`

Based on `tests/contract-fixtures/http/rate-limited.v1.json`:

```json
{
  "code": "rate_limited",
  "message": "Too many requests",
  "details": {
    "reason": "rate limit exceeded"
  }
}
```

429 responses also include:

- `X-Correlation-Id`
- `Retry-After`

## Operational Endpoints

Operational endpoints are public, unversioned, and not rate-limited.

### Health

```text
GET /health
```

Response, based on `tests/contract-fixtures/http/health.json`:

```json
{
  "status": "ok"
}
```

### Liveness

```text
GET /health/live
```

Response, based on `tests/contract-fixtures/http/health-live.json`:

```json
{
  "status": "ok",
  "check": "liveness"
}
```

### Readiness

```text
GET /health/ready
```

Response, based on `tests/contract-fixtures/http/health-ready.json`:

```json
{
  "status": "ok",
  "check": "readiness"
}
```

### Version

```text
GET /version
```

Response, based on `tests/contract-fixtures/http/version.json`:

```json
{
  "name": "CarbonOps API",
  "version": "0.1.0"
}
```

## Legacy Compatibility

Legacy routes currently remain supported:

- `GET /carbon-factors`
- `GET /carbon-factors/search`
- `GET /carbon-factors/{factorId}`
- `POST /carbon-factors/import`

The `/v1` routes are preferred for new consumers. Legacy and `/v1` carbon factor routes use the same response body shapes for equivalent requests.

There is no removal timeline for legacy routes yet.

## Security And Leakage Notes

- Public responses must not include API keys, configured key hashes, previous key hashes, revoked key hashes, or configured scopes.
- Do not paste real API keys into docs, issues, logs, screenshots, PR comments, or support tickets.
- `X-Correlation-Id` is safe for troubleshooting when it does not contain secrets.
- Import is an accepted boundary only: requests are validated and accepted but not persisted or executed yet.
- Read endpoints remain public by current design; make sure exposed factor data is appropriate for unauthenticated access.

## Relationship To Contract Evidence

Related evidence:

- `docs/architecture.md`
- `docs/api-contract.md`
- `docs/contract-parity.md`
- `docs/response-fixtures.md`
- `docs/openapi-contract-drift.md`
- `tests/contract-fixtures/http/`
- `tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json`

## Non-Goals

DOC-001 does not add:

- full generated OpenAPI documentation,
- generated SDK or client documentation,
- production readiness checklist,
- architecture documentation,
- runtime behavior changes,
- response fixture regeneration,
- route metadata changes.
