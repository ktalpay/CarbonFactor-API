# Observability Readiness (OPS-026)

Date: 2026-05-22

This document describes the structured logging baseline added for CarbonOps-API. It is intentionally narrow: OPS-026 establishes safe structured log events, while correlation IDs, durable audit events, and rate limiting remain separate follow-up work.

## Logging Baseline

CarbonOps-API uses the built-in ASP.NET Core / `Microsoft.Extensions.Logging` stack and the existing `Logging` configuration in `appsettings.json` and `appsettings.Development.json`.

The baseline keeps console logging compatible with local development and CI. It does not introduce Serilog, OpenTelemetry, database logging, or a custom logging sink.

New application log events use stable message templates with named properties:

- `CarbonOps API startup configuration loaded`
- `CarbonOps import authorization failed`
- `CarbonOps import validation failed`
- `CarbonOps import request accepted`

## Startup Logging

At startup, the API logs a safe configuration summary with counts and booleans only:

- `persistence_provider`
- `api_key_hash_configured`
- `previous_key_hash_count`
- `revoked_key_hash_count`
- `tenant_configured`
- `import_scope_count`

The startup log does not include key values, hashes, tenant values, scopes, connection strings, or raw configuration sections.

## Import Lifecycle Logging

Import authorization failures are logged at `Warning` with:

- `endpoint`
- `auth_failure_reason`
- `authentication_scheme`

Import validation failures are logged at `Information` with:

- `endpoint`
- `validation_failure_reason`

Accepted import boundary requests are logged at `Information` with:

- `endpoint`
- `authentication_scheme`
- `tenant_id`
- `batch_id`
- `validation_status`
- `accepted_records`
- `rejected_records`
- `error_count`
- `warning_count`
- `persisted`
- `import_execution`

The accepted import event is emitted after successful API key authentication and after the import boundary response has been produced. It does not change the public import response shape or import execution behavior.

## Secret Redaction And Non-Leakage

Application logs must not include:

- provided `X-Api-Key` values,
- computed API key hashes,
- configured current key hashes,
- configured previous key hashes,
- configured revoked key hashes,
- configured scope values,
- plaintext development key material,
- raw request bodies,
- raw header collections,
- raw query strings.

OPS-026 tests assert that import auth failure and accepted import logs do not contain API keys, hashes, configured scope values, plaintext dev key material, or the raw `X-Api-Key` header name.

## Current Boundaries

OPS-026 does not add:

- correlation ID middleware; OPS-027 covers this,
- durable audit event model; OPS-028 covers this,
- rate limiting; OPS-029 covers this,
- database logging,
- third-party logging providers,
- OpenTelemetry,
- read endpoint protection,
- auth behavior changes,
- public response shape changes.

## Validation

Relevant validation commands:

```bash
dotnet test src/dotnet/tests/CarbonOps.Api.Tests/CarbonOps.Api.Tests.csproj
dotnet test src/dotnet/CarbonOps.Api.sln
```

Known unrelated warning:

- `CarbonFactorEndpointsTests.HealthEndpointReturnsDeterministicPayload` currently triggers xUnit analyzer warning `xUnit1013` because it is a public method not marked with `[Fact]`.
