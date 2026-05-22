# Production Readiness (DOC-004)

Date: 2026-05-22

## Purpose And Scope

This document consolidates the current CarbonOps-API production readiness evidence, checklist items, operating assumptions, and known gaps before REL-001.

It is an input to REL-001. It is not a broad production readiness approval, release sign-off, deployment automation guide, or replacement for the operator runbook.

DOC-004 does not change runtime behavior, public API contracts, fixtures, metadata baselines, Docker packaging, scripts, or deployment infrastructure.

## Current Readiness Summary

Current verdict categories:

| Area | Verdict |
| --- | --- |
| Controlled/internal pilot readiness | Yes, under the assumptions in this document and `docs/production-runbook.md`. |
| Broad production readiness | No. Broad production is a no-go until the major gaps are addressed or formally accepted. |
| REL-001 review readiness | Yes, after DOC-004 is merged and validation evidence is available. |
| Python/.NET production equivalence | No. Python remains a legacy/local contract foundation, not a production-equivalent implementation of the modern .NET API. |
| Import persistence/execution readiness | No. Import requests are accepted at the boundary, but `persisted=false` and `import_execution="not_started"` remain current behavior. |

The current baseline is appropriate for a controlled/internal pilot only when deployment access, clients, data exposure, configuration, and operating procedures are tightly controlled.

## Evidence Map

Current readiness evidence:

| Evidence | What It Covers |
| --- | --- |
| `docs/architecture.md` | Repository architecture and boundaries across API, persistence, security, ingestion, observability, packaging, and parity. |
| `docs/public-endpoints.md` | Public endpoint guide for `/v1`, legacy carbon factor routes, protected import, operational endpoints, headers, and errors. |
| `docs/api-contract.md` | Public contract details for carbon factor reads, import boundary behavior, errors, versioning, and security config. |
| `docs/security-production-readiness.md` | SEC-007 security review for API key auth, tenant/scope config, revoke/rotation, leakage controls, and security gaps. |
| `docs/observability-readiness.md` | Structured logging, `X-Correlation-Id`, audit event model, rate limiting, and observability limits. |
| `docs/environment-config-hardening.md` | Production startup validation and secret-boundary rules from OPS-031. |
| `docs/deployment-packaging.md` | Docker packaging baseline, runtime configuration boundary, and packaging validation commands. |
| `docs/production-runbook.md` | Operator procedures for startup validation, health checks, smoke checks, key rotation, emergency revoke, logs, rollback, and troubleshooting. |
| `docs/contract-parity.md` | PT-001 cross-runtime contract parity baseline and Python-side discovery result. |
| `docs/response-fixtures.md` | PT-002 deterministic response fixture scope and update policy. |
| `docs/openapi-contract-drift.md` | PT-003 public metadata drift baseline and update policy. |
| `docs/production-parity-review.md` | PT-004 parity review, verdict, and Python modern API gap. |
| `tests/contract-parity/contract-parity-baseline.json` | Machine-readable parity baseline for route families, response field groups, error categories, headers, and non-goals. |
| `tests/contract-fixtures/http/` | Checked-in HTTP response fixtures for representative read, import, error, rate-limit, and operational responses. |
| `tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json` | Public route/method/auth/rate-limit metadata baseline. |

## Production Assumptions

Controlled/internal pilot readiness depends on these assumptions:

- CarbonOps-API is deployed as a single service/API boundary.
- The deployment image is built from the current root `Dockerfile`.
- Runtime configuration is provided externally and is not baked into the image.
- `ASPNETCORE_ENVIRONMENT=Production` is used for production-like runs, so production startup validation is enabled.
- API keys are high entropy and distributed through approved secret channels.
- Import clients are known, few, and controlled.
- Public read endpoint data is acceptable for unauthenticated access.
- In-process rate limiting is accepted for the controlled/internal pilot scope.
- Operators follow `docs/production-runbook.md`.
- Real secrets are not stored in the repository, issues, PRs, screenshots, or log snippets.
- Python is not treated as production-equivalent to the modern .NET API.

## Readiness Checklist

| Item | Status | Notes |
| --- | --- | --- |
| Public endpoint docs | Complete | `docs/public-endpoints.md` documents current routes and examples. |
| API contract docs | Complete | `docs/api-contract.md` documents current public contracts and invariants. |
| Architecture docs | Complete | `docs/architecture.md` captures current boundaries. |
| Developer setup docs | Complete | `docs/developer-setup.md` documents local validation and hygiene. |
| Security readiness review | Complete for baseline | `docs/security-production-readiness.md` allows controlled pilot, not broad exposure. |
| Environment config hardening | Complete for baseline | Production fails fast on unsafe security/rate-limit config. |
| Observability baseline | Complete for baseline | Structured logs, correlation id, audit events, and rate-limit signals exist. |
| Audit event model | Complete for baseline | Logging-backed audit events exist; durable persistence is not implemented. |
| Rate limiting boundary | Complete for baseline | In-process read/import policies exist; distributed throttling is not implemented. |
| API versioning | Complete for baseline | `/v1` carbon factor routes and legacy aliases are documented/tested. |
| Docker packaging | Complete for baseline | Root `Dockerfile`, `.dockerignore`, and packaging docs exist. |
| Production runbook | Complete for baseline | `docs/production-runbook.md` documents operator procedures. |
| Contract parity baseline | Complete for baseline | PT-001 manifest exists. |
| Response fixtures | Complete for baseline | PT-002 fixtures cover representative responses. |
| Metadata drift checks | Complete for baseline | PT-003 public metadata baseline exists. |
| Production parity review | Complete for baseline | PT-004 documents .NET coverage and Python gap. |
| Persistence production readiness review | Complete as review | DB-008 says persistence is ready for controlled integration work, not full production readiness. |
| Import persistence/execution | Not ready | Import remains accepted-only with `persisted=false` and `import_execution="not_started"`. |
| Durable audit persistence | Gap | Audit events are logging-backed only. |
| Distributed rate limiting | Gap | Current limiter is in-process only. |
| DB-backed token lifecycle | Gap | Token state is config-driven only. |
| Secret manager integration | Gap | Secrets must come from external runtime config; no built-in integration exists. |
| Generated OpenAPI/client strategy | Gap | No generated OpenAPI artifact or generated client publishing exists. |
| Python modern API parity | Gap | Python does not implement modern `/carbon-factors`, `/v1`, import, auth, rate-limit, correlation, or audit behavior. |

## Operational Checklist Before Controlled Pilot

Before a controlled/internal pilot:

- Build the Docker image from the root `Dockerfile`.
- Provide production environment variables externally.
- Configure the API key hash, tenant id, import scope, and rate limits.
- Confirm `Security__ApiKey__ImportEndpointKey` is absent.
- Confirm the production key hash and tenant id are not development placeholders.
- Run `GET /health`.
- Run `GET /version`.
- Run `GET /v1/carbon-factors`.
- Run a protected `POST /v1/carbon-factors/import` smoke check using an approved secret channel for the API key.
- Confirm logs include `correlation_id`.
- Confirm logging-backed audit events appear for accepted import or controlled failure scenarios.
- Confirm `429` rate-limit behavior if practical in the target environment.
- Confirm operators can access `docs/production-runbook.md`.
- Confirm rollback image and configuration paths are known before traffic is enabled.

## Security Checklist

Security checks before pilot:

- Do not configure plaintext API keys in Production.
- Use hash-only API key configuration through `Security__ApiKey__ImportEndpointKeyHash`.
- Do not use the development placeholder hash.
- Do not use `tenant-dev-001` or other development/test tenant placeholders.
- Keep the required import scope `carbon_factors:import`.
- Define the previous-hash rotation window process before rotating keys.
- Define the revoked-hash emergency process before issuing production credentials.
- Verify logs, error bodies, docs, tickets, and screenshots do not contain API keys, hashes, or raw secret material.
- Do not paste real keys into PRs, issues, chats, shell transcripts, or screenshots.
- Accept that there is no DB-backed token registry yet.
- Accept that there is no token expiry or generation API yet.

## Observability Checklist

Observability checks before pilot:

- Structured logs are enabled through the built-in ASP.NET Core logging stack.
- `X-Correlation-Id` is returned on success, auth failure, validation failure, and rate-limit responses.
- Logging-backed audit events are emitted for import auth failure, import validation failure, accepted import, and rate-limit rejection.
- Rate-limit rejection logs and audit events use safe fields only.
- Durable audit persistence is not implemented.
- OpenTelemetry and distributed tracing are not implemented.

## API And Contract Checklist

API and contract checks before pilot:

- `/v1` route coverage exists for carbon factor list, search, get-by-id, and import.
- Legacy unversioned carbon factor routes remain compatible.
- Operational endpoints remain unversioned.
- Public response shapes are covered by checked-in fixtures.
- Public route metadata is covered by the PT-003 baseline.
- No full generated OpenAPI schema artifact exists yet.
- No generated clients exist yet.

## Persistence And Import Checklist

Persistence and import checks before pilot:

- Default persistence remains in-memory with deterministic reference data.
- PostgreSQL is opt-in through `Persistence:UsePostgreSql=true`.
- PostgreSQL mode requires `Persistence:PostgreSql:ConnectionString` when enabled.
- DB-008 reviewed persistence as ready for controlled integration work, not full production readiness.
- OPS-031 and OPS-032 did not make PostgreSQL mandatory for Production.
- Import is accepted at the boundary only.
- Accepted import responses keep `persisted=false`.
- Accepted import responses keep `import_execution="not_started"`.
- No production import execution lifecycle exists yet.

## Go/No-Go Guidance

Controlled/internal pilot go guidance:

- Go only if every production assumption is accepted.
- Go only if the operational, security, observability, API/contract, and persistence/import checklists are complete or explicitly risk-accepted.
- Go only if operators have access to the production runbook and rollback paths.

Broad production go guidance:

- Broad production is a no-go at the current baseline.
- Broad production remains a no-go until durable audit persistence, distributed/edge throttling, DB-backed token lifecycle, secret manager integration, import persistence/execution, and any required read authorization are addressed or formally accepted by release owners.

## Known Gaps Before Broad Production

Known broad-production gaps:

- Durable audit persistence.
- Distributed or edge rate limiting for multi-instance deployments.
- DB-backed token lifecycle with owner, tenant/company, scopes, creation time, last-used time, expiry, revoke, and rotation workflow.
- Secret manager integration.
- Generated OpenAPI and generated client strategy.
- Python modern API parity if Python is intended to be production-facing.
- Import persistence and execution lifecycle.
- Read authorization and tenant filtering if read data becomes sensitive.
- External monitoring, alerting, and SIEM integration.
- Deployment automation and infrastructure provisioning.

## REL-001 Handoff

DOC-004 unblocks REL-001.

REL-001 should verify this evidence map, the readiness checklist, recent test results, and the go/no-go guidance. REL-001 should not rely only on documentation; it should run or review validation evidence for the current commit, confirm production assumptions with owners, and decide whether remaining gaps are accepted for the intended release scope.

REL-001 should not mark Python and .NET production-equivalent unless future work closes the Python modern API parity gap.

## Non-Goals

DOC-004 does not add:

- runtime changes,
- production approval,
- generated artifacts,
- deployment automation,
- secret manager integration,
- infrastructure provisioning,
- Dockerfile changes,
- checked-in fixture updates,
- OpenAPI artifact generation,
- generated clients,
- DB migrations,
- import execution or persistence.
