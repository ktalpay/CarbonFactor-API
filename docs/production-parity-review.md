# Production Parity Review (PT-004)

Date: 2026-05-22

## Purpose And Scope

PT-004 reviews the current CarbonOps-API contract parity position before REL-001. It uses the PT-001 parity baseline, PT-002 response fixtures, PT-003 public metadata drift check, and OPS-033 production runbook as evidence.

This is a review and handoff document. It does not claim broad production readiness, Python/.NET runtime equivalence, or full OpenAPI schema coverage. REL-001 remains the full production readiness review.

PT-004 does not change runtime behavior, public response contracts, route metadata, checked-in fixtures, Python runtime behavior, deployment packaging, auth, rate limiting, database behavior, or migrations.

## Evidence Summary

Primary evidence reviewed:

- PT-001 parity baseline manifest: `tests/contract-parity/contract-parity-baseline.json`
- PT-002 response fixtures: `tests/contract-fixtures/http/`
- PT-003 metadata drift baseline: `tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json`
- PT-001 documentation: `docs/contract-parity.md`
- PT-002 documentation: `docs/response-fixtures.md`
- PT-003 documentation: `docs/openapi-contract-drift.md`
- OPS-033 production runbook: `docs/production-runbook.md`
- Security production readiness review: `docs/security-production-readiness.md`
- API contract: `docs/api-contract.md`

The evidence shows the current `.NET` public HTTP contract is documented and tested through three complementary layers:

- PT-001 records the route families, field groups, error categories, headers, invariants, non-goals, and Python-side discovery result.
- PT-002 compares representative live `.NET` responses against deterministic checked-in JSON fixtures.
- PT-003 compares live ASP.NET Core endpoint metadata against a small public metadata baseline for route, method, version family, authentication, and rate-limit categories.

OPS-033 provides operator-facing procedures for deployment package use, production config, health checks, smoke checks, key rotation, emergency revoke, rate limiting, logs, audit events, troubleshooting, rollback, and known limitations.

## .NET Production Contract Status

The `.NET` API is the current production-facing implementation and source of truth for the modern CarbonOps HTTP contract.

Current route coverage:

- legacy read routes:
  - `GET /carbon-factors`
  - `GET /carbon-factors/search`
  - `GET /carbon-factors/{factorId}`
- `/v1` read routes:
  - `GET /v1/carbon-factors`
  - `GET /v1/carbon-factors/search`
  - `GET /v1/carbon-factors/{factorId}`
- protected import routes:
  - `POST /carbon-factors/import`
  - `POST /v1/carbon-factors/import`
- unversioned operational endpoints:
  - `GET /health`
  - `GET /health/live`
  - `GET /health/ready`
  - `GET /version`

Current contract behavior:

- Read routes remain public and are read-rate-limited.
- Import routes require `X-Api-Key`, use config-driven lowercase SHA-256 key hashes, and are import-rate-limited.
- Import tenant id and required import scope remain config-driven.
- Previous key hashes and revoked key hashes remain config-driven.
- Revocation is checked before current or previous key acceptance.
- `X-Correlation-Id` is returned on responses.
- Rate-limited responses include the existing 429 envelope and `Retry-After`.
- Logging-backed audit events cover import authorization failure, import validation failure, accepted import, and rate-limit rejection.
- Valid import responses remain `202 Accepted`, `persisted=false`, and `import_execution="not_started"`.

Coverage status:

- PT-002 locks representative response bodies for read, import, error, rate-limit, and operational responses.
- PT-003 locks public route metadata for legacy, `/v1`, and operational route families.
- PT-001 keeps the route families, headers, response field groups, and invariants visible as a cross-runtime parity baseline.

## Python-Side Parity Status

Python exists under:

```text
src/python
```

Current Python-facing contract modules include:

- `src/python/src/carbonops_api/contracts/models.py`
- `src/python/src/carbonops_api/contracts/errors.py`
- `src/python/src/carbonops_api/transport/envelope.py`

Current Python local routes are:

- `GET /health`
- `GET /factors`
- `GET /factors/{factor_id}`

Python currently has a legacy/local read-only `/factors` contract foundation. It covers factor DTOs, local read behavior, deterministic error structures, transport envelopes, and FastAPI OpenAPI metadata for the legacy local adapter.

Python does not yet implement the modern `.NET` `/carbon-factors` route family, `/v1` route family, import boundary, API key auth, tenant/scope/revoke/rotation behavior, rate limiting, correlation id response header behavior, or audit event behavior.

Python modern API parity is not complete. Full runtime parity is not achieved. This is acceptable only if Python is treated as a legacy/local contract foundation and not as a production-equivalent implementation of the current `.NET` API.

## Current Parity Verdict

Explicit verdict:

- `.NET` public HTTP contract: controlled and tested.
- `.NET` route metadata: controlled and tested.
- `.NET` response fixtures: controlled and tested.
- Python modern API parity: not complete.
- Cross-runtime production parity: partial, baseline only.
- Ready for REL-001 review: yes, with documented gaps.
- Ready to claim Python/.NET production equivalence: no.

The current evidence is strong enough to proceed into REL-001 because the `.NET` production-facing contract is bounded by PT-001, PT-002, and PT-003. It is not strong enough to describe Python and `.NET` as equivalent production runtimes.

## Risks Before Production Readiness

Known risks that remain before a broad production readiness claim:

- Python runtime gap for the modern `.NET` API surface.
- No generated clients.
- No full generated OpenAPI document or schema drift check.
- No durable audit persistence.
- No distributed rate limiting.
- No DB-backed token lifecycle.
- Read endpoints remain public by design.
- Import requests are accepted at the boundary but not persisted or executed.
- No external secret manager integration.
- No API gateway or WAF integration.
- Existing xUnit analyzer warning `xUnit1013` for `CarbonFactorEndpointsTests.HealthEndpointReturnsDeterministicPayload`, if still present.

These risks do not block PT-004 as a documentation review, but they must remain visible for REL-001 and follow-up planning.

## Required Follow-Ups

Follow-up mapping:

- REL-001: full production readiness review.
- DOC-004: production readiness documentation.
- Future Python parity work if Python is intended to become production-facing for the modern API surface.
- Future OpenAPI generation and schema drift checking if the project decides to publish full OpenAPI artifacts.
- Future generated client strategy if external consumers need managed SDKs.
- Future DB-backed token lifecycle for per-token owner, tenant, scopes, creation time, last-used time, expiry, revoke, and rotation workflow.
- Future durable audit persistence and retention policy.
- Future distributed or edge rate limiting for multi-instance deployments.

## Non-Goals Preserved

PT-004 preserves these non-goals:

- no runtime changes,
- no fixture regeneration,
- no OpenAPI artifact generation,
- no generated clients,
- no Python SDK redesign,
- no Python runtime parity implementation,
- no auth behavior changes,
- no rate limiting behavior changes,
- no route changes,
- no database or migration changes,
- no production readiness claim.

## Operator And Developer Guidance

Use PT-002 fixtures for response-shape drift. If a public response body changes intentionally, update the relevant fixture manually and document why.

Use the PT-003 metadata baseline for route, version family, auth category, and rate-limit category drift. If a public route or metadata category changes intentionally, update the baseline manually and keep it small.

Use the PT-001 parity baseline as the cross-runtime map. If Python is expanded toward modern `/carbon-factors` or `/v1` behavior, update the parity manifest and add tests before claiming broader parity.

Use the OPS-033 production runbook for operation. It is the current operator guide for packaging, runtime configuration, smoke checks, key rotation, emergency revoke, rate limiting, logging, audit events, rollback, and known limitations.

Treat Python as a legacy/local contract foundation unless future work explicitly changes that status.
