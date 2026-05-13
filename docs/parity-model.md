# Parity Model

The .NET and Python implementation paths should behave consistently at the
public API and domain contract level while remaining independent codebases.

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

The repository now includes `src/python` as the current Python implementation
root and `src/dotnet` as the initial .NET Clean Architecture solution skeleton.
The .NET path now includes the first carbon factor domain and public contract
baseline:

- `CarbonOps.Domain.CarbonFactor`
- `CarbonOps.Contracts.FactorDto`
- `CarbonOps.Contracts.FactorQuery`
- `CarbonOps.Contracts.ApiError`
- `CarbonOps.Contracts.FactorListResponse`
- `CarbonOps.Contracts.FactorDetailResponse`

The .NET contract models use idiomatic PascalCase C# members with documented
JSON names that preserve current Python public field parity, including
`factor_value` and `factor_unit`. The .NET path still does not add carbon
factor HTTP routes, persistence, CRUD behavior, or cross-language test
infrastructure.
