# Production Runbook (OPS-033)

Date: 2026-05-22

## Purpose And Scope

This runbook covers baseline production operation for CarbonOps-API as it exists after SEC-001 through SEC-007 and OPS-026 through OPS-032.

It is operator-focused and repository-grounded. It documents packaging, runtime configuration, health checks, smoke checks, key rotation, emergency revoke, rate limiting, logs, audit events, troubleshooting, rollback, and security handling rules.

For the consolidated readiness checklist and REL-001 handoff, see `docs/production-readiness.md` and `docs/production-readiness-review.md`.

This runbook does not add or cover:

- cloud provisioning,
- Kubernetes, Helm, Terraform, or Docker Compose production files,
- secret manager integration,
- external SIEM setup,
- CI/CD release automation,
- API gateway or WAF configuration,
- runtime behavior changes.

## Current Production Readiness Level

CarbonOps-API is ready for a controlled/internal pilot under restricted deployment assumptions:

- deployment access is limited to known operators,
- import clients are known and few,
- API keys are high entropy and distributed through an approved secret channel,
- public read endpoint data is acceptable for unauthenticated access,
- operators accept the current config-driven token, tenant, scope, revoke, and rotation model,
- operators understand imports are accepted at the boundary but are not persisted or executed.

CarbonOps-API is not ready for broad production exposure unless the remaining risks in `docs/security-production-readiness.md` are explicitly accepted or mitigated.

## Architecture Summary For Operators

Current runtime shape:

- Docker-packaged .NET 8 API.
- Carbon factor routes are available under `/v1` and legacy unversioned aliases.
- Protected import endpoints:
  - `POST /v1/carbon-factors/import`
  - `POST /carbon-factors/import`
- Public read endpoints:
  - `GET /v1/carbon-factors`
  - `GET /v1/carbon-factors/search`
  - `GET /v1/carbon-factors/{factorId}`
  - legacy unversioned equivalents
- Operational endpoints are unversioned:
  - `GET /health`
  - `GET /health/live`
  - `GET /health/ready`
  - `GET /version`
- Import auth is config-driven with `X-Api-Key` and lowercase SHA-256 key hashes.
- Tenant id and import scope are config-driven.
- Previous key hashes support rotation windows.
- Revoked key hashes support emergency deny listing.
- Structured logs, `X-Correlation-Id`, logging-backed audit events, and in-process rate limiting are enabled.
- The import boundary still returns `persisted=false` and `import_execution="not_started"`.

## Deployment Package

See `docs/deployment-packaging.md` for packaging details.

Build the container image from the repository root:

```bash
docker build -f Dockerfile -t carbonops-api:local .
```

Run a local Development-mode smoke container without production secrets:

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  carbonops-api:local
```

Then check:

```bash
curl -i http://localhost:8080/health
```

Packaging facts:

- The container listens on port `8080`.
- The runtime image uses the official .NET 8 ASP.NET runtime.
- The runtime stage uses the built-in non-root `APP_UID` user from the .NET runtime image.
- Secrets are not baked into the image.
- Publish output and Docker image layers are not committed to the repository.

## Required Runtime Configuration

Production containers must receive configuration from the runtime environment or an external secret/config mechanism.

Required production variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Security__ApiKey__ImportEndpointKeyHash=<production_lowercase_sha256_hex>
Security__ApiKey__ImportTenantId=<production_tenant_id>
Security__ApiKey__ImportEndpointScopes__0=carbon_factors:import
RateLimiting__Import__PermitLimit=10
RateLimiting__Import__WindowSeconds=60
RateLimiting__Import__QueueLimit=0
RateLimiting__Read__PermitLimit=60
RateLimiting__Read__WindowSeconds=60
RateLimiting__Read__QueueLimit=0
```

Optional production variables:

```text
Security__ApiKey__ImportEndpointPreviousKeyHashes__0=<production_lowercase_sha256_hex>
Security__ApiKey__RevokedKeyHashes__0=<production_lowercase_sha256_hex>
Persistence__UsePostgreSql=false
Persistence__PostgreSql__ConnectionString=<external_connection_string_if_enabled>
Persistence__PostgreSql__BootstrapOnStartup=false
Persistence__PostgreSql__BootstrapMode=ValidateOnly
```

Rules:

- Do not set `Security__ApiKey__ImportEndpointKey` in Production.
- Do not use the development placeholder hash from `appsettings.Development.json`.
- Do not use `tenant-dev-001` or other dev/test tenant placeholders in Production.
- Do not commit plaintext keys, hashes, tenant values, or connection strings.
- When PostgreSQL is enabled, use `BootstrapMode=ValidateOnly` to require pre-existing schema objects or `BootstrapMode=CreateMissing` to apply the checked-in non-destructive schema SQL for missing required objects.

## Startup Validation

OPS-031 runs production startup validation before the API serves requests. Invalid Production configuration fails fast.

Common startup failures:

- missing `Security:ApiKey:ImportEndpointKeyHash`,
- invalid current key hash format,
- development placeholder key hash,
- plaintext `Security:ApiKey:ImportEndpointKey` configured,
- missing, blank, or dev placeholder tenant id,
- missing scope list,
- scope list missing `carbon_factors:import`,
- blank scope item,
- invalid previous key hash,
- invalid revoked key hash,
- invalid rate limiting permit/window/queue values.

Troubleshooting approach:

1. Inspect the startup exception message.
2. Identify the config key and issue type.
3. Fix the value in the external runtime configuration source.
4. Restart the container.
5. Re-run `/health`.

Do not print, collect, paste, or attach secret values in tickets, logs, chat, issue comments, or PR comments.

## Health Checks

Operational endpoints are intentionally unversioned and are not rate-limited.

Expected healthy behavior is HTTP `200 OK`.

Endpoints:

```text
GET /health
GET /health/live
GET /health/ready
GET /version
```

`/version` returns the current API name and implementation version. It does not expose API keys, tenant values, or runtime secrets.

## Smoke Checks

Use placeholder values in shared docs and tickets. Never paste a real API key into a ticket or public shell transcript.

Health:

```bash
curl -i http://<host>:8080/health
```

Version:

```bash
curl -i http://<host>:8080/version
```

Public read route:

```bash
curl -i http://<host>:8080/v1/carbon-factors
```

Protected import boundary:

```bash
curl -i -X POST http://<host>:8080/v1/carbon-factors/import \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: <api_key_value_from_approved_secret_channel>" \
  -d '{
    "contract_version": "1.0",
    "batch_id": "smoke-batch-001",
    "source": {
      "source_system": "parser",
      "source_family": "electricity",
      "source_provider": "synthetic",
      "publication": "smoke-publication",
      "publication_version": "v1"
    },
    "factors": [
      {
        "external_factor_id": "smoke-ext-001",
        "source_family": "electricity",
        "source_provider": "synthetic",
        "category": "electricity",
        "activity": "grid",
        "factor_value": 0.1,
        "factor_unit": "kg"
      }
    ]
  }'
```

Expected accepted import behavior:

- HTTP `202 Accepted`,
- `persisted=false`,
- `import_execution="not_started"`,
- `audit.authentication_scheme="api_key"`,
- `audit.tenant_id` comes from runtime configuration.

## API Key Rotation Procedure

Current token state is configuration-driven:

- current key hash: `Security__ApiKey__ImportEndpointKeyHash`,
- previous key rotation window: `Security__ApiKey__ImportEndpointPreviousKeyHashes__0`,
- revoked hash list: `Security__ApiKey__RevokedKeyHashes__0`.

Procedure:

1. Generate a new high-entropy API key outside the repository.
2. Compute the lowercase SHA-256 hash outside the repository.
3. Set the new hash as `Security__ApiKey__ImportEndpointKeyHash`.
4. Move the old current hash to `Security__ApiKey__ImportEndpointPreviousKeyHashes__0` for the transition window.
5. Deploy or restart the container with the updated runtime configuration.
6. Verify the new key works against `POST /v1/carbon-factors/import`.
7. Verify the old key works only during the intended rotation window.
8. Migrate clients to the new key.
9. Remove the old hash from `Security__ApiKey__ImportEndpointPreviousKeyHashes__0`.
10. Deploy or restart again.
11. If the old key is compromised, add its hash to `Security__ApiKey__RevokedKeyHashes__0`.

Hash calculation example that avoids typing the key directly into shell history:

```bash
read -r -s CARBONOPS_API_KEY
printf '%s' "$CARBONOPS_API_KEY" | shasum -a 256 | awk '{print $1}'
unset CARBONOPS_API_KEY
```

Security rules:

- Never commit plaintext keys.
- Never paste plaintext keys into logs, tickets, issue comments, PR comments, or chat.
- Avoid posting computed hashes in public or shared channels.
- Treat key hash values as sensitive configuration.

## Key Compromise / Emergency Revoke Procedure

1. Determine the compromised key hash without exposing the plaintext key.
2. Add the compromised hash to `Security__ApiKey__RevokedKeyHashes__0` or the next available array index.
3. Deploy or restart the container.
4. Verify the compromised key returns `401 Unauthorized` or otherwise no longer works.
5. Generate and distribute a new high-entropy key through the approved secret channel.
6. Set the new current hash in `Security__ApiKey__ImportEndpointKeyHash`.
7. Review structured logs and audit events for spikes in:
   - `import.authorization_failed`,
   - `rate_limit.rejected`,
   - repeated `401` responses,
   - repeated `429` responses.
8. Document the incident without plaintext keys, configured hashes, tenant values, raw headers, raw query strings, or request bodies.

Revocation is checked before current or previous key acceptance, so a revoked hash must fail even if it also appears in the current or previous hash configuration.

## Rate Limiting Operation

OPS-029 adds in-process rate limiting.

Policy categories:

- import policy for `POST /carbon-factors/import` and `POST /v1/carbon-factors/import`,
- read policy for legacy and `v1` read routes.

Default limits:

```text
RateLimiting__Import__PermitLimit=10
RateLimiting__Import__WindowSeconds=60
RateLimiting__Import__QueueLimit=0
RateLimiting__Read__PermitLimit=60
RateLimiting__Read__WindowSeconds=60
RateLimiting__Read__QueueLimit=0
```

Rate-limited responses:

- HTTP `429 Too Many Requests`,
- JSON error envelope with `code="rate_limited"`,
- `details.reason="rate limit exceeded"`,
- `Retry-After` response header,
- `X-Correlation-Id` response header.

Current limitation: rate limiting is in-process. Multi-instance deployments need edge, gateway, or distributed throttling to enforce a global limit.

## Logs And Audit Events

The API uses built-in `Microsoft.Extensions.Logging` structured logs and a logging-backed audit sink.

Correlation:

- Requests may provide `X-Correlation-Id`.
- The API echoes valid IDs and generates one when missing or invalid.
- Use `correlation_id` to follow a request across application logs and audit events.

Current audit event types:

- `import.authorization_failed`,
- `import.validation_failed`,
- `import.accepted`,
- `rate_limit.rejected`.

Safe fields include:

- `event_type`,
- `occurred_at_utc`,
- `severity`,
- `endpoint`,
- `correlation_id`,
- `authentication_scheme`,
- `outcome`,
- `reason_code`,
- accepted import counters when available.

Forbidden fields in logs, audit events, tickets, and shared snippets:

- API keys,
- configured key hashes,
- previous key hashes,
- revoked key hashes,
- raw headers,
- raw query strings,
- request bodies,
- connection strings,
- plaintext tenant or secret configuration values unless the incident process explicitly requires a redacted private record.

## Common Troubleshooting Scenarios

### Container Does Not Start In Production

Likely causes:

- OPS-031 startup validation failed,
- required environment variables are missing,
- placeholder development values are still configured,
- rate limiting config is invalid.

Actions:

1. Read the startup exception message for key names and issue types.
2. Fix external runtime configuration.
3. Restart the container.
4. Check `/health`.

Do not paste config values into tickets.

### 401 On Import

Likely causes:

- missing `X-Api-Key`,
- wrong API key,
- revoked key hash,
- current/previous hash config mismatch,
- missing tenant config,
- missing or wrong import scope.

Actions:

1. Confirm the request path is `POST /v1/carbon-factors/import` or the legacy import route.
2. Confirm the key came from the approved secret channel.
3. Check logs by `correlation_id`.
4. Check runtime config keys without printing values.
5. If in a rotation window, confirm the old hash is still in previous hashes and not revoked.

### 429 On Read Or Import

Likely causes:

- request rate exceeded the read or import policy,
- a single client is retrying too quickly,
- multiple clients share the same effective remote IP partition.

Actions:

1. Read `Retry-After`.
2. Check logs for `rate_limit.rejected`.
3. Use `correlation_id` from the response.
4. Tune rate limit config if the limit is intentionally too low.
5. For multi-instance deployments, plan edge or distributed throttling.

### `/v1` Route 404

Likely causes:

- wrong path,
- using `/v1/health`, which does not exist,
- missing `/carbon-factors` segment.

Valid examples:

- `GET /v1/carbon-factors`
- `GET /v1/carbon-factors/search`
- `GET /v1/carbon-factors/{factorId}`
- `POST /v1/carbon-factors/import`

Operational endpoints remain unversioned.

### Valid Key Fails After Rotation

Likely causes:

- new hash was not set as current,
- old hash was removed from previous hashes too early,
- old hash was added to revoked hashes,
- hash was computed from a value with an unintended trailing newline.

Actions:

1. Recompute the hash using `printf '%s'`, not `echo`.
2. Check current, previous, and revoked hash placement without posting values.
3. Restart the container after config changes.
4. Re-test with `X-Correlation-Id` set to a known safe troubleshooting value.

### Missing `X-Correlation-Id`

This is not an error. The API generates a correlation id and returns it in the response header.

### Import Returns Accepted But Not Persisted

This is expected today. The import endpoint is a boundary acceptance and validation surface only:

- `persisted=false`,
- `import_execution="not_started"`.

### Public Read Endpoint Data Concern

Read endpoints are intentionally public in the current baseline. If read data becomes sensitive, stop broad exposure and prioritize read authorization and tenant-scoped filtering before customer-facing use.

## Rollback Guidance

Image rollback:

1. Choose the previously approved image version.
2. Keep Production runtime config external to the image.
3. Redeploy the previous image using the platform's approved process.
4. Run health and smoke checks.

Config rollback:

1. Restore the last known-good external runtime configuration.
2. Avoid restoring dev placeholders or plaintext key settings.
3. Restart the container.
4. Confirm `/health`, `/version`, and import smoke behavior.

Key rotation rollback:

1. If the new key fails, keep the previous known-good hash in `ImportEndpointPreviousKeyHashes` only if it is not compromised.
2. Move the known-good hash back to `ImportEndpointKeyHash` if operationally required.
3. Do not remove a compromised hash from `RevokedKeyHashes` just to restore traffic.
4. Document the rollback without secret values.

Avoid rolling back to insecure configuration.

## Security Handling Rules

- No secrets in the repository.
- No secrets in issues, PRs, comments, logs, screenshots, or shared terminal transcripts.
- Redact API keys, hashes, tenant values, connection strings, raw headers, raw query strings, and request bodies.
- Production config must come from runtime environment or an external secret/config mechanism.
- Do not build or share Docker images with embedded secrets.
- Do not use Development-mode containers for production traffic.
- Do not use the development placeholder hash or tenant id in Production.

## Known Limitations

Current limitations:

- no secret manager integration,
- no distributed rate limiting,
- no durable audit persistence,
- no DB-backed token registry,
- no token generation API,
- no token expiry scheduler,
- no deployment automation,
- no Kubernetes, Helm, Terraform, or cloud manifests,
- no external SIEM integration,
- no broad production readiness claim,
- import is not persisted or executed yet,
- read endpoints remain public.

## Follow-Up Mapping

OPS-033 unblocks PT-004 and DOC-004.

Remaining future hardening likely includes:

- durable audit persistence,
- distributed or edge-aware throttling,
- DB-backed token lifecycle,
- token generation and operator lifecycle APIs or tooling,
- OpenAPI and version lifecycle work,
- deployment automation,
- external secret management,
- read authorization and tenant-scoped filtering if read data becomes customer-specific or sensitive.
