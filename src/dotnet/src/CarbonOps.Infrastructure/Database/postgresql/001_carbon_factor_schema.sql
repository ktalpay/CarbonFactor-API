-- CarbonOps PostgreSQL schema baseline for carbon factor storage.
-- Safe to review and apply manually or through the non-destructive startup bootstrap.

BEGIN;

CREATE SCHEMA IF NOT EXISTS carbonops;

CREATE TABLE IF NOT EXISTS carbonops.carbon_factors (
    factor_id TEXT PRIMARY KEY,
    source_provider TEXT NOT NULL,
    source_family TEXT NULL,
    source_reference TEXT NULL,
    category TEXT NOT NULL,
    scope TEXT NULL,
    activity TEXT NOT NULL,
    region TEXT NULL,
    factor_year INTEGER NULL CHECK (factor_year IS NULL OR factor_year >= 0),
    factor_version TEXT NULL,
    factor_value NUMERIC(18,8) NOT NULL,
    factor_unit TEXT NOT NULL,
    notes TEXT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_carbon_factors_identity
    ON carbonops.carbon_factors (
        source_provider,
        COALESCE(source_family, ''),
        category,
        activity,
        COALESCE(region, ''),
        COALESCE(factor_year, -1),
        COALESCE(factor_version, ''),
        factor_unit
    );

CREATE INDEX IF NOT EXISTS ix_carbon_factors_query
    ON carbonops.carbon_factors (category, activity, region, factor_year);

CREATE INDEX IF NOT EXISTS ix_carbon_factors_source
    ON carbonops.carbon_factors (source_provider, source_family);

COMMIT;
