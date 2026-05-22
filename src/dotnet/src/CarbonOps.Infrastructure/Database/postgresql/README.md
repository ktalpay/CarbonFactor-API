# PostgreSQL schema baseline and bootstrap strategy (DB-004)

This folder contains a **reviewable SQL baseline** and deterministic manifest for carbon factor persistence.

## Files

- `schema-manifest.txt`: canonical ordered list of schema SQL scripts.
- `001_carbon_factor_schema.sql`: baseline schema script that creates `carbonops.carbon_factors` and supporting indexes.

## Migration/bootstrap strategy

- Runtime API startup executes schema SQL only when PostgreSQL mode is enabled and `Persistence:PostgreSql:BootstrapOnStartup=true` with `BootstrapMode=CreateMissing`.
- Runtime API startup can also validate required schema objects without creating them when `BootstrapMode=ValidateOnly`.
- Schema scripts are discovered through `schema-manifest.txt` in listed order.
- `PostgreSqlSchemaBootstrapPlanner` can be invoked explicitly by future tooling/CLI/tests to build an ordered application plan.
- `PostgreSqlSchemaSafetyValidator` rejects scripts containing destructive SQL tokens (`DROP`, `TRUNCATE`, `DELETE`, or `ALTER TABLE`).

## Local/dev usage

- Review SQL files and manifest in pull requests.
- Invoke planner/validator from explicit tooling or test harnesses to inspect what would be applied.
- Apply SQL to local PostgreSQL manually or through future dedicated migration tooling (not part of this task).

## Production safety assumptions

- This baseline is designed for explicit, review-first application workflows.
- Runtime startup bootstrap is opt-in and non-destructive.
- Production rollout should use controlled deployment steps with peer review and change approval.

## What DB-004 does not do

- No EF-generated migrations are introduced.
- No startup calls to `Database.Migrate()` or `Database.EnsureCreated()` are added.
- Seed/reference data strategy is defined in application code via `CarbonFactorReferenceDataCatalog` for deterministic sample/dev/test records.
- API startup still does not execute any seed writes automatically; controlled data loading remains a separate future concern.
- No destructive schema operations are introduced.
