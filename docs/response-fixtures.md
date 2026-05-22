# Response Fixture Comparison (PT-002)

Date: 2026-05-22

PT-002 adds deterministic response fixture comparison for key public CarbonOps-API contract shapes. The fixtures are small, checked-in JSON files intended for human review.

## Fixture Path

HTTP response fixtures live under:

```text
tests/contract-fixtures/http/
```

Current fixtures:

- `carbon-factors-list.v1.json`
- `carbon-factors-search-electricity.v1.json`
- `carbon-factor-f001.v1.json`
- `import-accepted.v1.json`
- `import-unauthorized-missing-api-key.v1.json`
- `search-invalid-year.v1.json`
- `factor-not-found.v1.json`
- `rate-limited.v1.json`
- `health.json`
- `health-live.json`
- `health-ready.json`
- `version.json`

## Fixture Scope

The .NET API fixture comparison covers:

- `GET /v1/carbon-factors`
- `GET /v1/carbon-factors/search?category=electricity&activity=grid%20electricity&region=US-WEST&year=2024`
- `GET /v1/carbon-factors/f-001`
- `POST /v1/carbon-factors/import` with a valid test request
- missing API key unauthorized response
- invalid query response
- not found response
- rate-limited response body
- health, liveness, readiness, and version responses

Legacy compatibility is covered by asserting `GET /carbon-factors` matches the same body as the `v1` list fixture.

## Normalization Rules

Fixture comparison parses JSON and serializes a canonical form with object properties sorted by ordinal property name.

Normalization does not remove contract fields and does not rewrite values.

Current accepted import fixture values are stable:

- `audit.audit_id` is deterministic for the representative request and remains locked in the fixture.
- `generated_at_utc` and `evaluated_at_utc` are `null` for the representative request.
- `persisted=false`.
- `import_execution="not_started"`.
- `audit.authentication_scheme="api_key"`.
- `audit.tenant_id` uses the configured test tenant.

The `Retry-After` response header is asserted separately for 429 responses and is not represented in the response body fixture.

## Update Policy

Only update fixtures when an intentional public contract change is made and reviewed.

When updating fixtures:

1. Run the focused fixture comparison tests first and confirm the failing diff is expected.
2. Update the relevant JSON fixture manually so the change is human-reviewable.
3. Validate all fixture JSON with `python -m json.tool`.
4. Run the .NET API and contract tests.
5. Update `docs/contract-parity.md` if the contract scope changes.

Do not generate fixtures during test execution into tracked paths.

## Python-Side Impact

The Python test suite verifies the fixture directory exists and that the checked-in fixture JSON is valid. Python does not compare its legacy `/factors` runtime responses against the modern `.NET` `/carbon-factors` fixtures in PT-002 because Python does not yet implement that surface.

## Non-Goals

PT-002 does not add:

- generated OpenAPI output,
- OpenAPI contract drift checks,
- generated clients,
- full Python runtime parity,
- public API response shape changes,
- runtime behavior changes.

## Follow-Up Mapping

- PT-003 adds OpenAPI-adjacent public metadata drift checking.
- PT-004 should perform the production parity review.
