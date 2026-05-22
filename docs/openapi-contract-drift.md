# OpenAPI Contract Drift Check (PT-003)

Date: 2026-05-22

PT-003 adds a deterministic public API metadata drift check for CarbonOps-API. The repository does not currently generate or publish a full OpenAPI document, so this task uses ASP.NET Core endpoint metadata as an OpenAPI-adjacent baseline.

## Discovery Result

The `.NET` API currently has no configured OpenAPI/Swagger generation path:

- no `Swashbuckle` package reference,
- no `Microsoft.AspNetCore.OpenApi` package reference,
- no `AddEndpointsApiExplorer`, `AddOpenApi`, `MapOpenApi`, or Swagger UI wiring,
- no generated OpenAPI artifact checked into the repository.

PT-003 intentionally does not add Swagger UI, generated clients, or a large generated OpenAPI file.

## Baseline Path

The public metadata baseline lives at:

```text
tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json
```

The baseline records:

- route path,
- HTTP method,
- route version family: `legacy`, `v1`, or `operational`,
- contract family: read, import, or operational,
- authentication category: `public` or `api_key`,
- rate limit category: `read`, `import`, or `none`,
- response status codes represented by endpoint example metadata where available,
- response headers that are part of the public boundary,
- the related PT-002 fixture when one exists.

## Drift Check Scope

The `.NET` API tests compare the checked-in baseline against the live ASP.NET Core `EndpointDataSource` route table.

The check covers:

- legacy carbon factor routes,
- `/v1` carbon factor routes,
- legacy and `/v1` import routes,
- unversioned health/liveness/readiness/version endpoints,
- read routes remaining public and read-rate-limited,
- import routes remaining API-key protected and import-rate-limited,
- operational endpoints remaining public and not rate-limited,
- `/v1/health` remaining absent from the public route set.

This drift check is intentionally metadata-focused. PT-002 remains responsible for deterministic response body fixture comparison.

## Update Policy

Update the baseline only when an intentional public API metadata change is made.

When updating:

1. Change the endpoint implementation or route metadata intentionally.
2. Update `tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json` by hand.
3. Keep the baseline small and human-reviewable.
4. Validate the JSON with `python -m json.tool`.
5. Run the `.NET` API and contract tests.
6. Update `tests/contract-parity/contract-parity-baseline.json` if route families changed.
7. Update PT-002 response fixtures only if response bodies changed intentionally.

Do not generate OpenAPI output into tracked paths during test execution.

## Relationship To PT-001 And PT-002

PT-001 records the cross-runtime route families and response field groups.

PT-002 locks representative response bodies with deterministic JSON fixtures.

PT-003 locks public route and metadata categories so route, versioning, authentication, and rate-limit drift is visible before a broader production parity review.

## Current Limitations

PT-003 does not provide:

- full generated OpenAPI JSON,
- OpenAPI schema drift checks,
- generated clients,
- Python runtime response comparison,
- production parity review,
- public API behavior changes.

If the project later adds full OpenAPI document generation, keep this metadata baseline or replace it with a small deterministic OpenAPI path/method/status comparison rather than committing a large noisy artifact.

## Follow-Up

PT-004 should perform the production parity review using PT-001, PT-002, and PT-003 as inputs.
