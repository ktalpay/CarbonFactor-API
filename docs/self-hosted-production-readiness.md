# Self-Hosted Production Readiness (PRD-001)

Date: 2026-05-22

## Purpose And Scope

PRD-001 redefines production readiness for CarbonOps-API.

The new production-ready target is self-hosted distribution readiness, not merely controlled/internal pilot readiness. A production-ready CarbonOps-API release should let a user download or fork the repository, choose either the .NET runtime or the Python runtime, provide their own database connection string, start the selected service, and have the selected runtime safely initialize or validate required database structures without maintainer-local assumptions or hidden setup.

This document is a definition and gap map. It does not implement DB bootstrap, migrations, runtime startup behavior, Docker changes, API behavior, service code, or generated artifacts.

The startup database bootstrap requirement is intentional: a self-hosted user should not need maintainer-local PostgreSQL setup knowledge to bring up the selected runtime.

CarbonOps-Parser is moving toward the same strategic distribution model: independent Python and .NET implementations that users can select. CarbonOps-API should follow that model, with both runtimes documented honestly until parity gaps are closed.

## Correct Production-Ready Definition

For CarbonOps-API, production-ready now means self-hosted distribution-ready:

- A user can download or fork the repository.
- A user can choose the .NET runtime or the Python runtime.
- A user can provide a database connection string through documented configuration.
- The selected service can start without maintainer-local PostgreSQL assumptions or hidden local setup.
- The selected runtime can initialize or validate required database structures safely on startup.
- The runtime can expose documented API endpoints after startup.
- Configuration, secrets, validation, and troubleshooting behavior are clear.
- Fresh-clone validation can prove the selected runtime starts against a user-provided database.

This definition is stricter than the REL-002 controlled/internal pilot checkpoint. Under this definition, the repository is not self-hosted production-ready yet.

## Historical Checkpoint Clarification

REL-002 remains valid as a historical controlled/internal pilot checkpoint:

- Controlled/internal pilot: GO with constraints.
- Broad production: NO-GO.
- Production deployment performed: NO.
- Runtime changes in REL-002: NO.

REL-002 is not final self-hosted production readiness. The earlier broad production NO-GO should now be understood as: CarbonOps-API is not self-hosted production-ready yet.

## Self-Hosted Readiness Criteria

| Criterion | Current Status | Notes |
| --- | --- | --- |
| Fresh clone works | Partial | Local validation is documented, but fresh-clone database-backed startup is not complete for both runtimes. |
| Runtime selection documented | Gap | The repo documents .NET as the modern API and Python as a legacy/local foundation, but not as selectable production runtimes. |
| .NET service start documented | Partial | Local and Docker starts are documented; PRD-002 adds startup DB bootstrap, while fresh-clone DB-backed start validation remains PRD-003. |
| Python service start documented | Partial | Python editable install and local tests are documented; production runtime start is not documented. |
| DB connection string config documented | Partial | .NET PostgreSQL connection string is documented. Python has no DB connection configuration. |
| DB bootstrap behavior implemented | Partial | PRD-002 adds .NET PostgreSQL startup validation/create-missing bootstrap. Python has no DB bootstrap. |
| DB bootstrap behavior tested | Partial | PRD-002 adds .NET unit/startup orchestration tests. Fresh-clone PostgreSQL startup proof and Python DB bootstrap tests remain missing. |
| Production config validation works | Partial | .NET Production validation exists for security/rate limits; DB bootstrap validation is not complete. Python production config validation is not present. |
| Secrets are external | Partial | Docs require external secrets; no secret manager integration is present. |
| Docker or equivalent packaging works | Partial | .NET Docker packaging exists. Python packaging/runtime distribution is not equivalent. |
| Health/readiness endpoints work | Partial | .NET has health/live/ready/version. Python has `/health` only. |
| Import/read behavior documented | Partial | .NET modern API is documented. Python does not implement modern `/carbon-factors` or import behavior. |
| Logs/correlation/audit/rate-limit behavior documented | Partial | .NET behavior is documented. Python lacks parity for these behaviors. |
| No local maintainer-specific settings required | Gap | This is not proven for fresh-clone DB-backed startup across both runtimes. |

## .NET Gap Map

Current .NET capabilities already present:

- Modern public API implementation under `src/dotnet`.
- `/v1` and legacy `/carbon-factors` routes.
- Protected import boundary with API key hash authentication.
- Production configuration validation for API key, tenant, scope, previous/revoked hashes, plaintext key rejection, and rate limiting.
- In-memory default persistence for deterministic local/test behavior.
- PostgreSQL opt-in through `Persistence:UsePostgreSql=true`.
- PostgreSQL connection string requirement through `Persistence:PostgreSql:ConnectionString`.
- EF Core `CarbonOpsDbContext`.
- EF Core repository and transaction boundary for read persistence.
- PostgreSQL schema SQL under `src/dotnet/src/CarbonOps.Infrastructure/Database/postgresql/`.
- `schema-manifest.txt` for deterministic schema script ordering.
- `PostgreSqlSchemaBootstrapPlanner` and `PostgreSqlSchemaSafetyValidator`.
- `Persistence:PostgreSql:BootstrapOnStartup` for startup validation/bootstrap opt-in.
- `Persistence:PostgreSql:BootstrapMode` with `ValidateOnly` and `CreateMissing`.
- Runtime startup bootstrap wiring for PostgreSQL mode when bootstrap is enabled.
- SQL safety validation that rejects destructive tokens such as `DROP`, `TRUNCATE`, `DELETE`, and `ALTER TABLE`.
- Docker packaging baseline.
- Health, liveness, readiness, and version endpoints.
- Structured logs, correlation id, audit events, and in-process rate limiting.

Current .NET gaps for self-hosted production readiness after PRD-002:

- No fresh-clone self-hosted startup test proves a user-provided PostgreSQL database becomes usable without manual schema application.
- No documented operator decision for when PostgreSQL is mandatory versus in-memory acceptable under the self-hosted target.
- Import remains `persisted=false` and `import_execution="not_started"`.
- Durable audit persistence is not implemented.
- DB credential lifecycle and external secret manager integration are not implemented.

Recommended .NET next implementation work:

- Add fresh-clone/start validation around the new PostgreSQL bootstrap path.
- Add opt-in integration evidence for fresh database startup when `CARBONOPS_POSTGRESQL_TEST_DSN` or equivalent is available.
- Update setup docs with a fresh-clone .NET self-hosted path.

## Python Gap Map

Current Python capabilities already present:

- Python package root under `src/python`.
- FastAPI adapter with `GET /health`.
- Legacy/local read-only routes:
  - `GET /factors`
  - `GET /factors/{factor_id}`
- Contract DTOs and error helpers.
- Synthetic in-memory sample data.
- Application repository port for factor lookup.
- In-memory repository adapter.
- Transport envelopes and status mapping.
- Python tests for the current local contract foundation.

Current Python gaps for self-hosted production readiness:

- No modern `/carbon-factors` or `/v1` route family.
- No `POST /carbon-factors/import` or `/v1/carbon-factors/import` boundary.
- No API key auth, tenant config, scope config, revoke, or rotation behavior.
- No rate limiting boundary.
- No `X-Correlation-Id` middleware/response header behavior.
- No logging-backed audit event model.
- No PostgreSQL or other DB connection string configuration.
- No DB repository adapter.
- No DB schema scripts, manifest, planner, safety validator, or bootstrap behavior.
- No production configuration validation equivalent to the .NET runtime.
- No Docker or equivalent self-hosted runtime packaging parity.
- No health/live/ready/version endpoint parity.
- No fresh-clone production-like startup documentation.

Python should not be described as modern API-ready or production-equivalent to .NET today. The repo needs an explicit product decision: either bring Python toward modern API/runtime parity, or document Python as a local/legacy contract foundation that is not part of the self-hosted production-ready target.

## Database Bootstrap Target Model

The expected database bootstrap model for self-hosted production readiness:

- On startup, the selected runtime checks the configured database.
- The runtime creates missing required tables/schema objects safely when configured to do so.
- Bootstrap is idempotent.
- Bootstrap does not drop user data.
- Bootstrap does not run destructive migrations automatically.
- Advanced or destructive migration behavior, if ever needed, must require an explicit operator-controlled flag or external migration workflow.
- The runtime logs a safe summary only, without connection strings, credentials, tenant values, API keys, or hashes.
- Invalid DB configuration fails fast before serving requests.
- PostgreSQL should be first-class because the current .NET persistence scope already centers on PostgreSQL.
- PRD-001 does not invent MySQL, SQL Server, or other database implementations.

## Runtime Startup Target Model

.NET target startup path:

- User clones or forks the repo.
- User chooses the .NET runtime.
- User provides required environment variables, including API key hash, tenant id, scope, rate limits, and PostgreSQL connection string when DB-backed mode is selected.
- Service starts with Production validation enabled.
- Startup validates or bootstraps PostgreSQL according to documented safe mode.
- Health/readiness endpoints confirm the service is ready.
- Fresh-clone validation commands prove startup succeeds without maintainer-local setup.

Python target startup path:

- User clones or forks the repo.
- User chooses the Python runtime.
- User installs the Python package and runtime dependencies.
- User provides required environment variables, including DB connection string and any security config required by the selected Python scope.
- Service starts without local maintainer assumptions.
- Startup validates or bootstraps required DB structures according to the same safe model.
- Health/readiness endpoints confirm the service is ready.
- Fresh-clone validation commands prove startup succeeds.

Required environment/config docs should cover:

- runtime selection;
- database connection string;
- secrets externalization;
- startup validation;
- bootstrap mode;
- health/readiness checks;
- troubleshooting for invalid DB config or bootstrap failure.

## Follow-Up Task Proposal

Proposed PRD roadmap:

1. PRD-002: .NET self-hosted DB bootstrap and startup readiness. Implemented in the .NET runtime baseline; fresh-clone proof remains PRD-003.
2. PRD-003: .NET fresh clone/start validation.
3. PRD-004: Python self-hosted DB/bootstrap design.
4. PRD-005: Python runtime parity or explicit Python scope decision.
5. PRD-006: Unified self-hosted setup docs.
6. PRD-007: Final self-hosted production readiness review.

Recommended order: start with .NET because it already has PostgreSQL configuration, EF Core repository support, schema SQL, a manifest, safety validation, Docker packaging, Production config validation, and the modern public API. Python should follow with a design/scope decision before implementation because it currently serves a legacy/local read-only `/factors` surface rather than the modern `.NET` API contract.

## Non-Goals

PRD-001 does not add:

- runtime changes;
- DB bootstrap implementation;
- DB migrations;
- Docker changes;
- generated artifacts;
- generated OpenAPI output;
- generated clients;
- secret manager integration;
- public response body changes;
- fixture changes;
- OpenAPI metadata baseline changes;
- a self-hosted production-ready claim.
