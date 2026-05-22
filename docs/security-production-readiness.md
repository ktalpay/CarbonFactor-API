# Security Production Readiness Review (SEC-007)

Date: 2026-05-22

This review covers the current API security model after SEC-001 through SEC-006. It is grounded in the .NET API implementation under `src/dotnet/src/CarbonOps.Api`, the import boundary contracts under `src/dotnet/src/CarbonOps.Contracts`, and the API security tests under `src/dotnet/tests/CarbonOps.Api.Tests`.

This document is a handoff review. It does not introduce new runtime behavior and does not claim broad production readiness.

## Current Protected Surface

Protected endpoint:

- `POST /carbon-factors/import`

Public endpoints by current design:

- `GET /carbon-factors`
- `GET /carbon-factors/search`
- `GET /carbon-factors/{factorId}`

The import endpoint is the only route protected by API key authentication today. The read endpoints remain public and SEC-006 explicitly tests that they do not require `X-Api-Key`.

Production implication: this is acceptable only if the read data exposed by these endpoints is intended to be public or safe for unauthenticated access. If read endpoints become customer-facing, tenant-specific, commercially sensitive, or sourced from non-public datasets, they need a separate authorization and tenant-filtering design before broad production exposure.

## Authentication Model

Current behavior:

- Clients authenticate to `POST /carbon-factors/import` with the `X-Api-Key` header.
- The active API key is configured as a lowercase SHA-256 hex hash in `Security:ApiKey:ImportEndpointKeyHash`.
- The API computes the SHA-256 hash of the provided key value and compares candidate hashes with `CryptographicOperations.FixedTimeEquals`.
- Missing or blank `ImportEndpointKeyHash` fails closed with `401 unauthorized`.
- Invalid current hash configuration fails closed with `401 unauthorized`.
- Plaintext active key comparison is not used by the current import authorization path.
- Missing, blank, whitespace-only, wrong, revoked, or duplicate `X-Api-Key` inputs do not authenticate.

Security assumption: configured API keys must be high entropy. SHA-256 hashing protects against accidental plaintext storage and response leakage, but it is not a substitute for strong key generation, secure secret distribution, or lifecycle controls.

## Tenant Boundary

Current behavior:

- Tenant identity comes from `Security:ApiKey:ImportTenantId`.
- Accepted import responses stamp the configured tenant id into `audit.tenant_id`.
- Accepted import responses stamp `audit.authentication_scheme` as `"api_key"`.
- SEC-006 tests that tenant-like values in the request payload, query string, and `X-Tenant-Id` header do not override the configured tenant id.
- Missing or blank tenant configuration fails closed with `401 unauthorized`.

Current limitations:

- There is no DB-backed tenant registry.
- There is no per-token tenant relationship.
- There is no tenant-scoped read filtering.
- The tenant boundary is config-driven metadata on the import boundary, not a full tenant authorization system.

Production implication: this is suitable for a controlled internal pilot where a single configured import identity maps to a known tenant/company context. It is not enough for multi-tenant production access without a durable tenant and token model.

## Scope And Permission Model

Current behavior:

- The required import scope is `carbon_factors:import`.
- Configured scopes come from `Security:ApiKey:ImportEndpointScopes`.
- Missing scope config fails closed.
- Blank or whitespace scope items fail closed.
- Wrong scope fails closed.
- Wrong-case scope fails closed because scope comparison is ordinal and case-sensitive.
- Public import responses do not expose configured or accepted scopes.

Current limitations:

- The scope list is global config for the import endpoint, not per-token metadata.
- There is no endpoint-level permission matrix beyond the import boundary.
- Read endpoints remain public and therefore have no read scopes yet.

Production implication: the current scope model locks down the import action but does not yet support multiple API clients with different permissions.

## Token Hashing, Revoke, And Rotation

Current config keys:

- Current key hash: `Security:ApiKey:ImportEndpointKeyHash`
- Previous key hashes: `Security:ApiKey:ImportEndpointPreviousKeyHashes`
- Revoked key hashes: `Security:ApiKey:RevokedKeyHashes`

Current behavior:

- The current key hash authenticates when tenant and scope checks pass.
- A previous key hash can authenticate during a rotation window when tenant and scope checks pass.
- Revoked hashes fail with `401 unauthorized` before current or previous hash acceptance.
- Invalid current, previous, or revoked hash configuration fails closed.
- Public responses do not disclose whether the current key or a previous key matched.

Current limitations:

- There is no DB-backed token registry.
- There are no token creation or generation APIs.
- There is no token expiry policy or scheduler.
- There is no per-token owner, tenant/company, scope, created time, or last-used metadata.
- There is no durable audit persistence for token decisions.
- Rotation and revocation are operator/configuration workflows, not first-class API workflows.

Production implication: config-driven revoke and rotation are enough for a narrow controlled pilot, but broad production exposure needs a token lifecycle system with durable state and operational procedures.

## Error Handling And Leakage Controls

Current behavior:

- Auth failures return `401 unauthorized`.
- Unauthorized responses use the existing JSON envelope:
  - `code`
  - `message`
  - `details`
- `details.reason` contains deterministic reason text.
- Error content type remains JSON.
- SEC-006 tests assert that error bodies do not leak:
  - provided API keys
  - current hashes
  - previous hashes
  - revoked hashes
  - plaintext development key material
  - wrong keys
  - configured scopes

Production implication: the current unauthorized error behavior is deterministic and avoids obvious key/config leakage. OPS-028 adds logging-backed audit events for auth and import boundary decisions without exposing secret material; broad production still needs durable audit persistence and retention policy.

## Import Response Invariants

Current valid import behavior:

- `POST /carbon-factors/import` returns `202 Accepted` for an accepted boundary request.
- Public response shape remains unchanged from the import boundary contract.
- `persisted=false`.
- `import_execution="not_started"`.
- `audit.authentication_scheme="api_key"`.
- `audit.tenant_id` comes from `Security:ApiKey:ImportTenantId`.

Current implication: the import endpoint is still a boundary acceptance and validation surface. It does not persist imported factor data or start an import execution lifecycle.

## Test Coverage

SEC-006 added `CarbonFactorImportSecurityTests`, which covers:

- Missing, blank, whitespace-only, duplicate, wrong, current, previous, and revoked API key inputs.
- Missing, blank, invalid-length, uppercase, and non-hex current hash configuration.
- Invalid and blank previous/revoked hash items.
- Current and previous key acceptance.
- Revocation precedence over current/previous acceptance.
- Tenant config failure and tenant id non-overridability by payload/query/header.
- Missing, blank, wrong-case, and insufficient import scopes.
- Error envelope shape, JSON content type, and non-leakage of keys/hashes/scopes.
- Public read endpoint access without `X-Api-Key`.
- Import response invariants for valid imports.

Validation commands used by the security lane:

```bash
dotnet test src/dotnet/tests/CarbonOps.Api.Tests/CarbonOps.Api.Tests.csproj
dotnet test src/dotnet/CarbonOps.Api.sln
```

Known test warning observed before this review:

- `CarbonFactorEndpointsTests.HealthEndpointReturnsDeterministicPayload` is a public method not marked with `[Fact]` and triggers xUnit analyzer warning `xUnit1013`. This is not part of the auth behavior but should be cleaned up in a later test-maintenance pass.

## Production Readiness Verdict

### Ready For Controlled/Internal Pilot

The current security model is suitable for a controlled internal pilot when:

- the API is deployed in a restricted environment,
- import clients are known and few,
- API keys are high entropy and distributed through a secure operator-controlled channel,
- the config-driven tenant/scope/revoke/rotation model is operationally acceptable,
- public read endpoints expose only data that is safe to serve without authentication,
- operators understand that accepted imports are not persisted or executed yet.

### Not Ready For Broad Production Exposure

The current model is not ready for broad production exposure because it lacks durable token lifecycle, tenant registry, durable audit event persistence, distributed or edge-aware rate limiting, secret-management guidance, and production runbooks.

### Required Before Broad Production

Required before broad production:

- DB-backed token registry.
- Per-token metadata: owner, tenant/company, scopes, `created_at`, `last_used_at`.
- Stronger token lifecycle controls around hashing, storage, rotation, revocation, and emergency disablement.
- Token generation, rotation, and revoke APIs or a documented operator workflow.
- Token expiry policy and enforcement.
- Durable audit event persistence for auth decisions and import boundary actions.
- Distributed or edge-aware rate limiting for protected and potentially expensive endpoints.
- Production logging deployment guidance for the existing structured logging and correlation id baseline.
- Secret management and deployment guidance for non-development environments.
- Tenant-scoped read filtering if read endpoints become protected customer endpoints or expose tenant-specific data.

### Recommended Hardening

Recommended hardening:

- Document exact production configuration requirements for `Security:ApiKey`.
- Add deployment checks that fail startup or health readiness when required security config is invalid.
- Add auth decision audit events that include non-secret token identifiers once a token registry exists.
- Add explicit operational runbooks for key compromise, rotation window closure, and revoked hash rollout.
- Add monitoring and alerting around auth failures, import rejection spikes, and rate-limit activity.
- Add a test-maintenance pass for the existing `xUnit1013` warning.

## OPS-026 Through OPS-031 Operational Baseline

See `docs/observability-readiness.md` for the current structured logging, request correlation, audit event, in-process rate limiting, and versioned-route observability baseline. See `docs/environment-config-hardening.md` for production configuration validation and secret-boundary rules. OPS-026 adds safe named-field logs for startup configuration summary and import lifecycle events, OPS-027 adds `X-Correlation-Id` middleware and `correlation_id` logging scope enrichment, OPS-028 adds a logging-backed audit event model, OPS-029 adds import/read rate limiting boundaries, OPS-030 adds `/v1` carbon factor routes while preserving legacy unversioned routes, and OPS-031 adds production startup validation for security-critical config. Durable audit persistence, external audit export, distributed rate limiting, API gateway/WAF integration, future version lifecycle tooling, secret manager integration, and deployment packaging remain follow-up work.

## Follow-Up Task Mapping

SEC-007 unblocks OPS-026. The current risk mapping is:

- OPS-026 structured logging baseline: needed for production-grade logs with secret redaction.
- OPS-027 correlation id middleware: needed for request tracing across import boundary handling.
- OPS-028 audit event model: needed for durable auth and import boundary decision records.
- OPS-029 rate limiting boundary: needed to reduce brute force, replay, and import abuse risk.
- OPS-030 API versioning strategy: establishes `/v1` carbon factor routes and keeps legacy compatibility; future version lifecycle tooling may still be needed before broad public or customer-facing API commitments.
- OPS-031 environment config hardening: adds production config validation and secret-boundary guidance; secret manager integration remains future deployment work.
- OPS-032 deployment packaging baseline: needed for reproducible deployment and environment separation.
- OPS-033 production runbook: needed for key compromise, rotation, revoke, incident, and rollback procedures.
- Future SEC task: likely needed for DB-backed token registry and full token lifecycle if not represented by an existing issue.

## Non-Goals Preserved

This review did not implement:

- DB-backed token registry.
- Database migrations.
- Token creation or generation APIs.
- Admin UI.
- Token expiry scheduler.
- Audit persistence.
- External identity provider integration.
- Read endpoint protection.
- Tenant-scoped read filtering.
- Public import response shape changes.
- Authentication behavior changes.
- Import persistence or execution behavior.
