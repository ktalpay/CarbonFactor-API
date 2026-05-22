# Persistence production readiness review (DB-008)

Date: 2026-05-21  
Scope: `src/dotnet` persistence-related implementation and tests only.

Update: PRD-002 adds optional .NET PostgreSQL startup schema validation/bootstrap through `Persistence:PostgreSql:BootstrapOnStartup` and `Persistence:PostgreSql:BootstrapMode`. This DB-008 review remains useful historical context, but statements about no runtime startup schema behavior should be read as pre-PRD-002 baseline notes.

## Verdict

Current persistence is **not fully production-ready**. It is **ready for controlled integration work** with explicit operational procedures and additional SEC/ING hardening.

## Current implemented persistence capabilities

- PostgreSQL baseline schema script exists for `carbonops.carbon_factors` and is manifest-driven (`schema-manifest.txt`).
- Schema bootstrap plan generation and SQL safety validation exist and are explicit/opt-in.
- API composition chooses persistence mode via `Persistence:UsePostgreSql`.
- Default mode remains in-memory, with deterministic shared reference sample data.
- PostgreSQL mode requires an explicit connection string and resolves EF Core repository + transaction boundary.
- EF Core mapping exists for domain `CarbonFactor` to `carbonops.carbon_factors`.
- Transaction boundary exists in two modes:
  - EF Core transaction boundary for PostgreSQL mode.
  - No-op transaction boundary for in-memory mode.
- PostgreSQL integration tests are opt-in locally via `CARBONOPS_POSTGRESQL_TEST_DSN`.

## Explicit non-goals currently preserved

- No automatic schema application at API startup.
- No `Database.Migrate()` / `EnsureCreated()` startup behavior.
- No automatic seed writes at startup.
- No parser ingestion or import endpoints.
- No authentication/authorization implementation in persistence path.

## Production-readiness checklist (status)

- [x] Deterministic baseline schema artifact exists.
- [x] Schema script ordering and safety gate exist.
- [x] Persistence mode is explicit and defaults to non-DB.
- [x] PostgreSQL mode fails fast when connection string is missing.
- [x] Repository reads are covered by unit + PostgreSQL integration tests.
- [x] Transaction boundary abstraction exists and is wired.
- [ ] Migration/change-management runbook for production rollout is not yet defined.
- [ ] Secret-rotation and credential-lifecycle procedures are not yet documented.
- [ ] Observability for DB health (query latency/error-rate dashboards, alerts) is not yet defined.
- [ ] Connection resiliency policy (timeouts/retries/pooling posture) is not yet explicitly documented.
- [ ] Data governance and authoritative source ingestion controls are not yet implemented.

## Configuration and secret handling review

- PostgreSQL is opt-in via `Persistence:UsePostgreSql`.
- `Persistence:PostgreSql:ConnectionString` is mandatory when enabled.
- For production and shared environments, connection strings should be sourced from secret managers or environment variables, **not committed in appsettings files**.
- Current code-level validation is minimal and appropriate for fail-fast startup, but does not enforce richer constraints (TLS requirements, role scoping, host allow-listing).

## Schema application / migration model review

- Current model is review-first, explicit SQL baseline + manifest planning.
- Runtime startup intentionally does not mutate schema.
- Safety validator blocks common destructive SQL tokens.
- Gap: long-term versioned migration governance (roll-forward policy, approvals, rollback strategy, ownership) should be formalized before production rollout.

## Transaction boundary review

- EF Core transaction boundary wraps operation, `SaveChangesAsync`, commit, and rollback-on-error.
- In-memory mode uses no-op boundary for deterministic local/testing behavior.
- Boundary is sufficient for current single-DbContext operations.
- Gap: future multi-repository/multi-resource workflows will need explicit idempotency/outbox or compensating strategy (outside DB-008 scope).

## Data correctness / reference data review

- Reference data catalog is deterministic sample data and used by in-memory repository.
- No claim of real-world factor correctness is established by current code.
- No authoritative source ownership, provenance workflow, or validation policy is implemented yet.
- Gap handoff to ING-001: controlled ingestion pipeline with provenance, validation, and reconciliation.

## Test coverage review

- Unit coverage exists for in-memory repository, EF repository behavior, schema planner, and schema safety validator.
- PostgreSQL integration tests verify baseline schema application path and repository read behavior under opt-in DSN.
- Gap: no repository write-path behavior tests yet (expected given current read-focused scope).

## Operational risks

1. **Configuration drift risk**: environment enables PostgreSQL without mature secret/rotation processes.
2. **Schema change process risk**: absent formal migration runbook may cause inconsistent rollout discipline.
3. **Observability gap**: no documented DB SLO/alert posture for query errors or latency.
4. **Data provenance gap**: no ingestion governance for authoritative factor lifecycle.

## Security risks to hand off to SEC-001

- Enforce production secret sourcing/rotation policy for DB credentials.
- Define least-privilege DB role requirements and separation between app/runtime and schema-change roles.
- Define transport security requirements for DB connections (TLS expectations, cert validation).
- Add security review checklist for connection string handling in CI/CD and local development.

## Ingestion/data-loading risks to hand off to ING-001

- Define authoritative source intake and versioning model.
- Add data validation, provenance metadata, and change-audit strategy.
- Define idempotent load/update semantics for factor records.
- Define reconciliation/quality checks between staged and published datasets.

## Recommended next tasks

1. **SEC-001**: persistence credential + DB access hardening policy and implementation constraints.
2. **ING-001**: controlled ingestion path and provenance model (no runtime auto-seed).
3. **DB follow-up**: migration operations runbook (script review, approvals, rollout/rollback steps).
4. **OPS follow-up**: persistence observability baseline (health checks, metrics, alerts).

## Notes for current merge decision

This task provides a repository-grounded readiness review and keeps runtime behavior unchanged. The persistence layer should be treated as suitable for controlled integration environments, not as complete production readiness.
