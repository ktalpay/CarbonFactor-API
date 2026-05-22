# Parity Model

The .NET and Python implementation paths should behave consistently at the
public API and domain contract level while remaining independent codebases.

See `docs/contract-parity.md` and
`tests/contract-parity/contract-parity-baseline.json` for the PT-001 baseline
manifest that records current route families, response field groups, error
envelopes, header behavior, and explicit PT-001 non-goals.

See `docs/response-fixtures.md` and `tests/contract-fixtures/http/` for the
PT-002 checked-in response fixtures that lock representative public response
shapes.

## Parity Goals

- Same documented routes or route equivalents.
- Same CarbonFactor field semantics.
- Same supported query filters.
- Same deterministic error codes for equivalent failures.
- Same response status expectations.
- Same public limitation language.

## Allowed Differences

The implementations may differ in:

- project layout
- framework choice
- dependency injection style
- test framework
- serialization internals
- deployment packaging

These differences are acceptable when public behavior remains aligned.

## Evidence Expectations

Future parity evidence should include:

- contract tests shared by behavior, not source imports
- route and OpenAPI comparison where applicable
- sample request/response fixtures
- error-case comparison
- documentation updates when behavior changes

## Current Status

PT-001 adds a tested baseline manifest. The current Python side remains a
legacy read-only contract foundation for `/factors` and transport envelopes.
The current .NET side is the source of truth for the modern `/carbon-factors`,
`/v1`, protected import, security, observability, and operational endpoint
surface. PT-002 adds representative response fixture comparison for the .NET
public surface and fixture availability checks for Python. OpenAPI drift checks,
generated clients, and Python SDK redesign remain out of scope.

The repository now includes `src/python` as the current Python implementation
root and `src/dotnet` as the initial .NET Clean Architecture solution skeleton.
The .NET path now includes the first carbon factor domain and public contract
baseline plus application-layer lookup behavior:

- `CarbonOps.Domain.CarbonFactor`
- `CarbonOps.Contracts.FactorDto`
- `CarbonOps.Contracts.FactorQuery`
- `CarbonOps.Contracts.ApiError`
- `CarbonOps.Contracts.FactorListResponse`
- `CarbonOps.Contracts.FactorDetailResponse`
- `CarbonOps.Application.Factors.ICarbonFactorRepository`
- `CarbonOps.Application.Factors.CarbonFactorUseCases`
- `CarbonOps.Infrastructure.InMemoryCarbonFactorRepository`

The .NET contract models use idiomatic PascalCase C# members with documented
JSON names that preserve current Python public field parity, including
`factor_value` and `factor_unit`. The .NET application lookup behavior mirrors
the current Python use-case rules: deterministic id ordering, exact-match
filters for `category`, `activity`, `region`, and `year`, `not_found` for
missing factors, and `invalid_query` for non-positive years or unsupported
extra filters. The .NET path now uses a deterministic in-memory repository
adapter for local composition and now exposes equivalent minimal HTTP lookup
routes for list, get-by-id, and search behavior. Persistence, CRUD behavior,
and cross-language test infrastructure remain out of scope.

Current parity evidence for the carbon factor endpoints is intentionally
contract-focused:

- `GET /carbon-factors` returns `{ "factors": [...], "total": <int> }`
- `GET /carbon-factors/{factorId}` returns `{ "factor": {...} }`
- `GET /carbon-factors/search` accepts only `category`, `activity`, `region`,
  and `year` exact-match filters
- factor payload fields remain `id`, `source`, `category`, `activity`,
  `factor_value`, `factor_unit`, `region`, `year`, and `notes`
- deterministic error envelopes remain
  `{ "code": <string>, "message": <string>, "details": {...} }`
- `not_found` errors carry `details.id`
- `invalid_query` errors carry `details.reason` for unsupported filters,
  non-positive years, and non-integer `year` values
