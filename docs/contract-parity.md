# Contract Parity Baseline (PT-001)

Date: 2026-05-22

PT-001 establishes a minimal cross-runtime contract parity baseline for CarbonOps-API. It records the public route families, response field groups, error envelope categories, headers, and non-goals that future Python and .NET parity work should use as the shared contract map.

The baseline lives at:

```text
tests/contract-parity/contract-parity-baseline.json
```

Response fixture comparison added by PT-002 is documented in
`docs/response-fixtures.md`. The checked-in fixtures live under:

```text
tests/contract-fixtures/http/
```

OpenAPI-adjacent public metadata drift checking added by PT-003 is
documented in `docs/openapi-contract-drift.md`. The checked-in metadata
baseline lives under:

```text
tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json
```

## Purpose

The repository has two runtime roots:

- `.NET`: current hardened API implementation under `src/dotnet`.
- `Python`: legacy/local contract foundation under `src/python`.

PT-001 does not force both runtimes to implement the same surface today. Instead, it makes the current split explicit and testable so future parity tasks can detect scope creep and drift.

## .NET Contract Source Of Truth

The current public API source of truth is the .NET implementation:

- contracts: `src/dotnet/src/CarbonOps.Contracts`
- API routes and middleware: `src/dotnet/src/CarbonOps.Api`
- contract tests: `src/dotnet/tests/CarbonOps.Contracts.Tests`
- API shape/security/route tests: `src/dotnet/tests/CarbonOps.Api.Tests`

The .NET API currently owns:

- legacy `/carbon-factors` read routes,
- `/v1/carbon-factors` read routes,
- legacy and `/v1` protected import routes,
- unversioned operational endpoints,
- API key auth, tenant/scope/revoke/rotation behavior,
- correlation id response headers,
- rate limit response headers,
- import boundary audit metadata,
- error envelopes.

## Python-Side Discovery Result

Python package root:

```text
src/python
```

Current Python-facing contract modules:

- `src/python/src/carbonops_api/contracts/models.py`
- `src/python/src/carbonops_api/contracts/errors.py`
- `src/python/src/carbonops_api/transport/envelope.py`

Current Python local routes:

- `GET /health`
- `GET /factors`
- `GET /factors/{factor_id}`

The Python side has a read-only legacy contract foundation for factor DTOs, query fields, deterministic errors, and transport envelopes. It does not yet implement the modern `.NET` `/carbon-factors` import boundary, API key auth, rate limiting, audit event behavior, or `/v1` route surface.

PT-001 intentionally does not redesign the Python package or add a generated client.

## Route Families In Baseline

The parity baseline records these public route families:

- `.NET` legacy read routes:
  - `GET /carbon-factors`
  - `GET /carbon-factors/search`
  - `GET /carbon-factors/{factorId}`
- `.NET` `v1` read routes:
  - `GET /v1/carbon-factors`
  - `GET /v1/carbon-factors/search`
  - `GET /v1/carbon-factors/{factorId}`
- `.NET` legacy import route:
  - `POST /carbon-factors/import`
- `.NET` `v1` import route:
  - `POST /v1/carbon-factors/import`
- `.NET` unversioned operational routes:
  - `GET /health`
  - `GET /health/live`
  - `GET /health/ready`
  - `GET /version`
- Python legacy local adapter routes:
  - `GET /health`
  - `GET /factors`
  - `GET /factors/{factor_id}`

## Response Field Groups

The baseline records field groups for:

- factor items,
- list/search responses,
- factor detail responses,
- accepted import responses,
- import audit metadata,
- import validation messages,
- error envelopes,
- Python transport envelopes,
- health/liveness/readiness/version responses.

The accepted import invariant remains:

- `persisted=false`
- `import_execution="not_started"`
- `audit.authentication_scheme="api_key"`

Public responses must not expose API keys, configured key hashes, previous hashes, revoked hashes, or configured scopes.

## Error Envelope Categories

The baseline records:

- `unauthorized` / HTTP `401`
- `invalid_query` / HTTP `400`
- `not_found` / HTTP `404`
- `rate_limited` / HTTP `429`

Error envelope fields remain:

- `code`
- `message`
- `details`

## Header Contract Surface

The baseline records:

- `X-Correlation-Id` on responses,
- `Retry-After` on rate-limited responses,
- `X-Api-Key` as required only for import routes.

## Current Non-Goals

PT-001 does not add:

- generated clients,
- generated OpenAPI output,
- OpenAPI contract drift checks,
- byte-for-byte response fixture comparison,
- broad Python SDK redesign,
- public API response shape changes,
- .NET runtime behavior changes.

## Follow-Up Mapping

- PT-002 adds response fixture comparison for representative public contract
  shapes.
- PT-003 adds OpenAPI-adjacent public metadata drift checking.
- PT-004 should perform the production parity review.
