# Observability Readiness (OPS-026 / OPS-027 / OPS-028 / OPS-029 / OPS-030)

Date: 2026-05-22

This document describes the structured logging, request correlation, audit event, rate limiting, and versioned-route observability baseline added for CarbonOps-API. It is intentionally narrow: OPS-026 establishes safe structured log events, OPS-027 adds request correlation IDs, OPS-028 adds an audit event model with a logging-backed sink, OPS-029 adds in-process rate limiting, and OPS-030 adds `/v1` carbon factor route aliases. Durable audit persistence, distributed throttling, API gateway/WAF integration, billing quotas, and future API version lifecycles remain separate follow-up work.

## Logging Baseline

CarbonOps-API uses the built-in ASP.NET Core / `Microsoft.Extensions.Logging` stack and the existing `Logging` configuration in `appsettings.json` and `appsettings.Development.json`.

The baseline keeps console logging compatible with local development and CI. It does not introduce Serilog, OpenTelemetry, database logging, or a custom logging sink.

New application log events use stable message templates with named properties:

- `CarbonOps API startup configuration loaded`
- `CarbonOps import authorization failed`
- `CarbonOps import validation failed`
- `CarbonOps import request accepted`
- `CarbonOps rate limit rejected`

## Correlation ID Baseline

OPS-027 adds request correlation through the `X-Correlation-Id` header.

Current behavior:

- If a request provides a valid `X-Correlation-Id`, the API reuses that value.
- If a request omits `X-Correlation-Id`, the API generates a new value with `Guid.NewGuid().ToString("N")`.
- If a request provides a blank, whitespace-only, malformed, too-long, or duplicate `X-Correlation-Id`, the API treats it as invalid ambiguity and generates a new value.
- Every response includes `X-Correlation-Id`, including success responses, validation failures, and auth failures.
- Invalid or duplicate incoming raw correlation values are not echoed in the response.
- The accepted/generated correlation id is stored on `HttpContext.Items` for later internal access.

Current validation rules:

- non-empty,
- no whitespace,
- maximum length `128`,
- only ASCII letters, digits, hyphen, underscore, dot, and colon.

Logging integration:

- Request logs use a structured logging scope with named field `correlation_id`.
- OPS-026 import lifecycle logs are enriched by the scope without adding `correlation_id` manually to each log call.
- Raw `X-Correlation-Id` header collections are not logged.

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

## Audit Event Baseline

OPS-028 adds internal audit event records for security-relevant import boundary decisions. Audit events are emitted through `IAuditEventSink`; the default implementation is `LoggingAuditEventSink`, which writes named structured fields through the dedicated `CarbonOps.Api.Audit` logger category.

Current audit event types:

- `import.authorization_failed`
- `import.validation_failed`
- `import.accepted`
- `rate_limit.rejected`

Audit events include safe structured fields where applicable:

- `event_id`
- `event_type`
- `occurred_at_utc`
- `severity`
- `endpoint`
- `correlation_id`
- `authentication_scheme`
- `tenant_id`
- `outcome`
- `reason_code`
- `batch_id`
- `validation_status`
- `accepted_records`
- `rejected_records`
- `error_count`
- `warning_count`
- `persisted`
- `import_execution`

Auth failure audit events use normalized reason codes, such as `invalid_api_key`, instead of raw secret-bearing inputs. Validation failure audit events use the API error code. Accepted import audit events include the configured tenant id and import boundary counters after successful authentication and validation.

This is not durable audit persistence. Audit events are not written to database tables, a queue, or an external SIEM/exporter in OPS-028.

## Rate Limiting Boundary

OPS-029 adds in-process ASP.NET Core rate limiting for the current API surface.

Policy categories:

- Import policy: `POST /carbon-factors/import` and `POST /v1/carbon-factors/import`
- Read policy: legacy and `v1` `GET /carbon-factors`, `GET /carbon-factors/search`, and `GET /carbon-factors/{factorId}` routes

Default configuration:

```json
{
  "RateLimiting": {
    "Import": {
      "PermitLimit": 10,
      "WindowSeconds": 60,
      "QueueLimit": 0
    },
    "Read": {
      "PermitLimit": 60,
      "WindowSeconds": 60,
      "QueueLimit": 0
    }
  }
}
```

Partitioning is intentionally simple for OPS-029:

- remote IP address when available,
- a fixed fallback partition for test/server contexts where remote IP is unavailable.

The limiter does not use API keys, hashes, raw headers, raw query strings, request bodies, or correlation IDs as partition keys.

Rate-limited responses return `429 Too Many Requests` with the existing JSON error-envelope style:

- `code="rate_limited"`
- `message="Too many requests"`
- `details.reason="rate limit exceeded"`

The response includes `Retry-After` and `X-Correlation-Id`. If the request supplied a valid correlation id, the response echoes it; otherwise the correlation middleware generates one before rate limiting runs.

Rate limit rejections are logged at `Warning` with safe fields:

- `endpoint`
- `rate_limit_policy`
- `reason_code="rate_limit_exceeded"`
- `correlation_id` from the logging scope

OPS-029 also emits a logging-backed audit event with `event_type="rate_limit.rejected"`, `outcome="failure"`, and `reason_code="rate_limit_exceeded"`.

Operational endpoints are not rate-limited in OPS-029:

- `GET /health`
- `GET /health/live`
- `GET /health/ready`
- `GET /version`

## Versioned Route Observability

OPS-030 adds `v1` carbon factor routes while preserving legacy unversioned routes.

Import logs and import audit events use the actual request path in the `endpoint` field. This means:

- legacy import requests report `endpoint="/carbon-factors/import"`,
- `v1` import requests report `endpoint="/v1/carbon-factors/import"`.

Rate limit rejection logs and `rate_limit.rejected` audit events also report the matched legacy or `v1` endpoint path. Raw query strings are not logged.

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

OPS-026/OPS-027 tests assert that import auth failure and accepted import logs do not contain API keys, hashes, configured scope values, plaintext dev key material, raw invalid correlation id values, or the raw `X-Api-Key` header name.

OPS-028/OPS-029 tests assert that audit event and rate-limit rejection logs do not contain provided API keys, configured hashes, configured scope values, plaintext development key material, raw headers, raw query strings, or request bodies.

## Current Boundaries

OPS-026/OPS-027/OPS-028/OPS-029/OPS-030 do not add:

- durable audit persistence,
- external audit/SIEM integration,
- distributed rate limiting,
- Redis or database-backed quota tracking,
- API gateway/WAF configuration,
- billing or plan quotas,
- future version lifecycle tooling or OpenAPI generation overhaul,
- database logging,
- third-party logging providers,
- OpenTelemetry,
- distributed tracing exporters,
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
