# Environment Config Hardening (OPS-031)

Date: 2026-05-22

OPS-031 adds production-oriented startup validation for CarbonOps-API configuration. The scope is intentionally narrow: it validates security-critical settings and keeps secret values out of startup failures and logs. It does not add a secret manager, deployment packaging, or a production runbook. OPS-032 deployment packaging is documented in `docs/deployment-packaging.md`.

## Environment Model

Development and testing:

- Safe non-production placeholders are allowed.
- Local development can continue using `appsettings.Development.json`.
- Tests can continue overriding configuration through `WebApplicationFactory`.

Production:

- Security-critical configuration is validated during startup before the API serves requests.
- Invalid production configuration fails fast.
- Failure messages identify configuration keys and issue types, not configured values.

## Production Validation Rules

`Security:ApiKey:ImportEndpointKeyHash`:

- required,
- must be lowercase SHA-256 hex,
- must not equal the known development placeholder hash from `appsettings.Development.json`.

`Security:ApiKey:ImportEndpointKey`:

- must not be configured in production,
- plaintext API key configuration remains unsupported for the active auth path.

`Security:ApiKey:ImportTenantId`:

- required,
- non-blank,
- must not use obvious development or test placeholders such as `tenant-dev-001`.

`Security:ApiKey:ImportEndpointScopes`:

- required,
- must include `carbon_factors:import`,
- must not contain blank values.

`Security:ApiKey:ImportEndpointPreviousKeyHashes`:

- optional,
- every configured value must be lowercase SHA-256 hex.

`Security:ApiKey:RevokedKeyHashes`:

- optional,
- every configured value must be lowercase SHA-256 hex.

`RateLimiting:Import` and `RateLimiting:Read`:

- `PermitLimit` must be greater than zero,
- `WindowSeconds` must be greater than zero,
- `QueueLimit` must be zero or greater.

## Secret Boundary

Production configuration should be supplied by environment-specific configuration, environment variables, or a deployment secret mechanism outside the repository. OPS-031 does not integrate a secret manager.

The API must not log or return:

- plaintext API keys,
- configured API key hashes,
- previous or revoked hashes,
- tenant id values,
- configured scope values,
- connection strings,
- raw configuration sections.

Startup logging remains a safe summary with booleans and counts only.

## Environment Variable Shape

ASP.NET Core environment variables can represent nested configuration with double underscores.

Examples without real secrets:

```text
Security__ApiKey__ImportEndpointKeyHash=<lowercase_sha256_hex>
Security__ApiKey__ImportTenantId=<production_tenant_id>
Security__ApiKey__ImportEndpointScopes__0=carbon_factors:import
Security__ApiKey__ImportEndpointPreviousKeyHashes__0=<lowercase_sha256_hex>
Security__ApiKey__RevokedKeyHashes__0=<lowercase_sha256_hex>
RateLimiting__Import__PermitLimit=10
RateLimiting__Import__WindowSeconds=60
RateLimiting__Import__QueueLimit=0
RateLimiting__Read__PermitLimit=60
RateLimiting__Read__WindowSeconds=60
RateLimiting__Read__QueueLimit=0
```

Do not set `Security__ApiKey__ImportEndpointKey` in production.

## Persistence Note

OPS-031 does not require PostgreSQL for production startup. The current import endpoint still returns `persisted=false` and `import_execution="not_started"`, so production persistence hardening remains limited by the current feature scope.

Future persistence, deployment, and runbook tasks should decide whether production environments must fail startup when PostgreSQL is disabled or missing.

## Non-Goals

OPS-031 does not add:

- real secrets,
- cloud or vendor-specific secret manager integration,
- Docker or deployment packaging,
- CI/CD environment provisioning,
- production runbooks,
- database migrations,
- auth behavior changes for valid configuration,
- public response body changes.

OPS-032 deployment packaging baseline is covered in `docs/deployment-packaging.md`. OPS-033 covers the production runbook.
