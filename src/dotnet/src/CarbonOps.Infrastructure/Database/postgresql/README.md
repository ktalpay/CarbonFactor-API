# PostgreSQL schema baseline (DB-001)

This folder contains a **reviewable SQL baseline** for carbon factor persistence.

## File

- `001_carbon_factor_schema.sql`: creates `carbonops.carbon_factors` and supporting indexes.

## Notes

- Runtime API behavior is unchanged: this schema is not wired into application startup.
- SQL uses `IF NOT EXISTS` guards and is non-destructive.
- `factor_id` maps to the existing factor identity used in API/domain contracts.
