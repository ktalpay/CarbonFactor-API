# Architecture

Date: 2026-05-22

## Purpose And Scope

This document is the current architecture overview for the CarbonOps-API repository. It describes the repository layout and the present boundaries across API, persistence, security, ingestion, observability, packaging, and cross-runtime parity.

This is documentation only. It does not change runtime behavior, public response contracts, fixtures, route metadata, Docker packaging, migrations, or deployment automation.

This document does not claim broad production readiness. See `docs/security-production-readiness.md`, `docs/persistence-production-readiness.md`, `docs/production-runbook.md`, and the future REL-001 review for production readiness decisions.

## Repository Layout

Current top-level repository areas:

- `src/dotnet`: modern `.NET` implementation root and current source of truth for the public HTTP API.
- `src/python`: legacy/local Python contract foundation and read-only `/factors` adapter.
- `tests/contract-parity`: cross-runtime parity baseline manifest from PT-001.
- `tests/contract-fixtures/http`: deterministic response fixtures from PT-002.
- `tests/contract-fixtures/openapi`: OpenAPI-adjacent public metadata baseline from PT-003.
- `docs`: architecture, contract, readiness, runbook, packaging, and parity documentation.
- `scripts/ops`: local operator/developer helper scripts, including packaging validation and local worker utilities.
- `Dockerfile`: multi-stage .NET 8 container packaging baseline for the API.
- `.dockerignore`: Docker build-context exclusions for local caches, generated outputs, and local environment files.

## .NET API Architecture

The `.NET` solution under `src/dotnet` follows a Clean Architecture-style split:

- `CarbonOps.Api`: ASP.NET Core composition root, middleware registration, minimal API endpoint mapping, production configuration validation, rate limiting setup, structured logging/audit sink registration, and operational endpoints.
- `CarbonOps.Application`: use cases and application ports, including carbon factor lookup and import boundary validation.
- `CarbonOps.Contracts`: public DTOs, query contracts, error envelopes, parser import request contracts, and import boundary response contracts.
- `CarbonOps.Domain`: domain `CarbonFactor` concept and domain-level validation.
- `CarbonOps.Infrastructure`: in-memory repository, EF Core/PostgreSQL repository, PostgreSQL schema planning/safety helpers, transaction boundary implementations, and reference data catalog.

Runtime composition starts in `CarbonOps.Api/Program.cs`:

```text
WebApplicationBuilder
  -> bind API key options
  -> register logging-backed audit sink
  -> register production configuration validator
  -> register rate limiting
  -> register carbon factor services and persistence adapters
  -> build app
  -> validate Production configuration
  -> install correlation id, routing, rate limiting, and API error mapping middleware
  -> map operational endpoints
  -> map carbon factor endpoints
```

Minimal API endpoint mapping lives in `CarbonOps.Api/CarbonFactorEndpoints.cs` and `CarbonOps.Api/OperationalEndpoints.cs`.

Carbon factor routes are mapped twice:

- preferred `/v1/carbon-factors` routes,
- legacy `/carbon-factors` compatibility aliases.

Operational endpoints remain unversioned:

- `GET /health`
- `GET /health/live`
- `GET /health/ready`
- `GET /version`

## Public HTTP Boundary

The current public HTTP boundary is documented in `docs/public-endpoints.md` and `docs/api-contract.md`.

Current route categories:

- Public read endpoints:
  - `GET /v1/carbon-factors`
  - `GET /v1/carbon-factors/search`
  - `GET /v1/carbon-factors/{factorId}`
  - legacy unversioned equivalents
- Protected import endpoints:
  - `POST /v1/carbon-factors/import`
  - `POST /carbon-factors/import`
- Public operational endpoints:
  - `GET /health`
  - `GET /health/live`
  - `GET /health/ready`
  - `GET /version`

Public contract behavior:

- Read endpoints are public and use the `read` rate-limit policy.
- Import endpoints require `X-Api-Key` and use the `import` rate-limit policy.
- Operational endpoints are public, unversioned, and not rate-limited.
- Error responses use the shared JSON envelope: `code`, `message`, and `details`.
- Every response includes `X-Correlation-Id`.
- Rate-limited responses include `Retry-After`.

Representative response bodies are locked by PT-002 fixtures under `tests/contract-fixtures/http/`. Public route and metadata categories are locked by PT-003 under `tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json`.

## Ingestion Boundary

The current ingestion boundary is the parser import endpoint:

```text
POST /v1/carbon-factors/import
POST /carbon-factors/import
```

The request contract is defined in `CarbonOps.Contracts`:

- `ParserCarbonFactorBatchImportRequest`
- `ParserSourceMetadataDto`
- `ParserCarbonFactorImportItem`
- `ParserProvenanceMetadataDto`

Current import behavior:

- validates batch shape and row-level factor input,
- accepts requests with at least one valid row,
- returns `202 Accepted` for accepted boundary requests,
- emits deterministic validation warnings/errors,
- includes import audit metadata in the public response,
- stamps `audit.tenant_id` from runtime configuration after successful API key authentication,
- stamps `audit.authentication_scheme="api_key"`,
- keeps `persisted=false`,
- keeps `import_execution="not_started"`.

The import endpoint is a boundary acceptance and validation surface. It does not yet run a full import execution lifecycle, write imported records, publish data, or persist durable audit events.

See `docs/ingestion-production-readiness.md` for ingestion readiness gaps and `docs/public-endpoints.md` for request/response examples.

## Persistence Architecture

Persistence is implemented behind application ports in `CarbonOps.Application` and adapters in `CarbonOps.Infrastructure`.

Current persistence modes:

- Default mode: in-memory repository backed by deterministic reference data.
- PostgreSQL mode: opt-in through `Persistence:UsePostgreSql=true`.

PostgreSQL support includes:

- EF Core `CarbonOpsDbContext`,
- repository adapter for reading carbon factor records,
- EF Core transaction boundary,
- PostgreSQL schema baseline SQL under `src/dotnet/src/CarbonOps.Infrastructure/Database/postgresql/`,
- schema manifest and bootstrap planning helpers,
- SQL safety validation helpers,
- opt-in PostgreSQL integration tests through `CARBONOPS_POSTGRESQL_TEST_DSN`.

Runtime composition chooses the mode in `CarbonFactorServiceCollectionExtensions`:

- if `Persistence:UsePostgreSql` is false or missing, the API uses in-memory services,
- if enabled, `Persistence:PostgreSql:ConnectionString` is required and startup fails fast when missing.

Production boundaries:

- OPS-031 does not require PostgreSQL in Production.
- OPS-032 documents `Persistence__UsePostgreSql=false` as an acceptable runtime setting for the current baseline.
- DB-008 concludes persistence is ready for controlled integration work, not full production readiness.
- The current import boundary still returns `persisted=false` and `import_execution="not_started"`.
- Runtime startup intentionally does not auto-apply schemas with `Database.Migrate()` or `EnsureCreated()`.

See `docs/persistence-production-readiness.md`, `docs/environment-config-hardening.md`, and `docs/deployment-packaging.md`.

## Security Boundary

The protected import boundary uses config-driven API key authentication.

Current security configuration:

- provided key header: `X-Api-Key`,
- current key hash: `Security:ApiKey:ImportEndpointKeyHash`,
- previous key hashes for rotation: `Security:ApiKey:ImportEndpointPreviousKeyHashes`,
- revoked key hashes: `Security:ApiKey:RevokedKeyHashes`,
- import tenant id: `Security:ApiKey:ImportTenantId`,
- import scopes: `Security:ApiKey:ImportEndpointScopes`,
- required scope: `carbon_factors:import`.

Current security behavior:

- API keys are verified by SHA-256 lowercase hex hash comparison.
- Revoked key hashes fail before current or previous key acceptance.
- Missing or invalid current/previous/revoked hash configuration fails closed.
- Tenant id is configuration-derived and not request-controllable.
- Scope comparison is deterministic and case-sensitive.
- Public responses do not disclose API keys, hashes, previous/revoked hashes, or configured scopes.

Production configuration hardening:

- Production startup validation requires safe security and rate limit config.
- `Security:ApiKey:ImportEndpointKey` plaintext config is rejected in Production.
- Development placeholder key hash and tenant id values are rejected in Production.
- Secret values are not logged in startup summaries or validation errors.

Current security limitations:

- no DB-backed token registry,
- no per-token owner/tenant/scope metadata,
- no token generation API,
- no expiry scheduler,
- no durable auth decision audit persistence,
- no read endpoint protection or tenant-scoped read filtering.

See `docs/security-production-readiness.md` and `docs/environment-config-hardening.md`.

## Observability Boundary

Observability is intentionally built on the platform-provided ASP.NET Core logging stack.

Current observability capabilities:

- structured logs with stable message templates,
- startup configuration summary with booleans/counts only,
- `X-Correlation-Id` middleware,
- `correlation_id` logging scope,
- import authorization failure logs,
- import validation failure logs,
- accepted import logs,
- rate-limit rejection logs,
- internal audit event model,
- logging-backed audit sink with dedicated `CarbonOps.Api.Audit` category.

Current audit event types:

- `import.authorization_failed`
- `import.validation_failed`
- `import.accepted`
- `rate_limit.rejected`

Current observability limitations:

- no durable audit persistence,
- no database audit table,
- no external SIEM/exporter integration,
- no OpenTelemetry or distributed tracing exporters,
- no database logging.

See `docs/observability-readiness.md`.

## Rate Limiting And Operational Boundaries

OPS-029 adds in-process ASP.NET Core fixed-window rate limiting.

Policy categories:

- `read`: legacy and `/v1` `GET /carbon-factors`, `/search`, and `/{factorId}` routes.
- `import`: legacy and `/v1` `POST /carbon-factors/import` routes.
- `none`: operational endpoints.

Default config:

```text
RateLimiting:Import:PermitLimit=10
RateLimiting:Import:WindowSeconds=60
RateLimiting:Import:QueueLimit=0
RateLimiting:Read:PermitLimit=60
RateLimiting:Read:WindowSeconds=60
RateLimiting:Read:QueueLimit=0
```

Partitioning is intentionally simple:

- remote IP when available,
- fixed fallback partition in test/server contexts.

The limiter does not partition by API key, raw headers, raw query strings, request bodies, or correlation id. Multi-instance deployments still need edge or distributed throttling for broad production exposure.

Operational endpoints are not rate-limited:

- `GET /health`
- `GET /health/live`
- `GET /health/ready`
- `GET /version`

See `docs/observability-readiness.md` and `docs/production-runbook.md`.

## Deployment And Package Boundary

OPS-032 adds the packaging baseline.

Packaging shape:

- repository root Docker build context,
- root `Dockerfile`,
- multi-stage .NET 8 restore/build/publish/runtime image,
- Release publish,
- no apphost,
- runtime port `8080`,
- runtime uses the non-root `APP_UID` user from the .NET runtime image,
- `.dockerignore` excludes generated outputs, local caches, local environment files, and task artifacts.

Runtime configuration remains external to the image. Production containers must receive security, rate limit, optional persistence, and ASP.NET runtime settings through environment variables or another external configuration channel.

Production startup validation runs before serving requests when `ASPNETCORE_ENVIRONMENT=Production`.

Current deployment non-goals:

- no Kubernetes,
- no Helm,
- no Terraform,
- no cloud-provider manifests,
- no deployment automation,
- no API gateway/WAF config,
- no external secret manager integration.

See `docs/deployment-packaging.md`, `docs/environment-config-hardening.md`, and `docs/production-runbook.md`.

## Cross-Runtime And Parity Boundary

The `.NET` implementation is the current source of truth for the modern public API.

The Python implementation under `src/python` remains a legacy/local read-only contract foundation:

- `GET /health`,
- `GET /factors`,
- `GET /factors/{factor_id}`.

Python does not yet implement the modern `.NET` `/carbon-factors` route family, `/v1` routes, import boundary, API key auth, tenant/scope/revoke/rotation behavior, rate limiting, correlation id behavior, or audit event behavior.

Parity evidence chain:

- PT-001: `tests/contract-parity/contract-parity-baseline.json`
- PT-002: `tests/contract-fixtures/http/`
- PT-003: `tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json`
- PT-004: `docs/production-parity-review.md`

Current parity verdict:

- `.NET` public HTTP contract is controlled and tested.
- `.NET` route metadata is controlled and tested.
- `.NET` response fixtures are controlled and tested.
- Python modern API parity is not complete.
- Cross-runtime production parity is partial and baseline-only.
- Python/.NET full production equivalence is not claimed.

## Current Limitations

Current known limitations:

- Import is accepted at the boundary but not persisted or executed.
- Read endpoints remain public by design.
- No DB-backed token lifecycle.
- No per-token owner/tenant/scope/last-used metadata.
- No durable audit persistence.
- No distributed rate limiting.
- No full generated OpenAPI schema artifact.
- No generated clients.
- No external secret manager integration.
- No Kubernetes/Helm/Terraform/deployment automation.
- No broad production readiness claim.
- Python modern API parity is not complete.

## Follow-Up Map

Planned or likely follow-ups:

- DOC-003: local development and validation workflows.
- DOC-004: production readiness documentation and operational assumptions.
- REL-001: full production readiness review.
- Future SEC task: DB-backed token registry and token lifecycle.
- Future OPS task: durable audit persistence and retention policy.
- Future OPS task: distributed or edge-aware rate limiting for multi-instance deployments.
- Future API task: full OpenAPI generation/schema drift if the project chooses to publish OpenAPI artifacts.
- Future parity task: Python modern API parity if Python becomes production-facing.

## Relationship To Other Docs

Related docs:

- `docs/developer-setup.md`
- `docs/public-endpoints.md`
- `docs/api-contract.md`
- `docs/ingestion-production-readiness.md`
- `docs/persistence-production-readiness.md`
- `docs/security-production-readiness.md`
- `docs/environment-config-hardening.md`
- `docs/observability-readiness.md`
- `docs/deployment-packaging.md`
- `docs/production-runbook.md`
- `docs/contract-parity.md`
- `docs/response-fixtures.md`
- `docs/openapi-contract-drift.md`
- `docs/production-parity-review.md`
