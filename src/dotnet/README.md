# CarbonOps-API .NET

This directory contains the initial .NET 8 Clean Architecture solution skeleton
for CarbonOps-API.

## Current Status

- `CarbonOps.Api.sln` defines the .NET solution boundary.
- Source projects are separated into domain, application, contracts,
  infrastructure, and API layers.
- `CarbonOps.Domain` contains the first `CarbonFactor` domain model baseline.
- `CarbonOps.Contracts` contains the first factor DTO, query, response, and
  deterministic error contract models.
- `CarbonOps.Application` contains the first carbon factor repository port and
  list, get-by-id, and search use cases.
- `CarbonOps.Infrastructure` now contains a deterministic in-memory carbon
  factor repository adapter and DI registration for local/dev/testing use.
- `CarbonOps.Api` now exposes minimal HTTP endpoints for factor list, get-by-id,
  and search behavior using those use cases.
- Test projects provide smoke, boundary, and focused model contract tests.
- No persistence, authentication, authorization, parser execution, background
  jobs, or production hardening is implemented.
- The current repository behavior remains provided by the Python implementation
  under `src/python`.

## Solution Layout

```text
src/dotnet/
  CarbonOps.Api.sln
  src/
    CarbonOps.Domain/
    CarbonOps.Application/
    CarbonOps.Contracts/
    CarbonOps.Infrastructure/
    CarbonOps.Api/
  tests/
    CarbonOps.Domain.Tests/
    CarbonOps.Application.Tests/
    CarbonOps.Contracts.Tests/
    CarbonOps.Infrastructure.Tests/
    CarbonOps.Api.Tests/
```

## Project Boundaries

- `CarbonOps.Domain`: domain concepts only; no ASP.NET, EF Core, HTTP, or
  infrastructure dependencies.
- `CarbonOps.Application`: use-case boundary; may reference domain and
  contracts, but not infrastructure or API. Carbon factor lookup use cases
  depend on the `ICarbonFactorRepository` port.
- `CarbonOps.Contracts`: public contract models; no infrastructure or API
  dependency.
- `CarbonOps.Infrastructure`: adapter implementations when added; must not
  reference the API project.
- `CarbonOps.Api`: composition root and ASP.NET Core host.

## Current Infrastructure Adapter

`CarbonOps.Infrastructure` now provides:

- `InMemoryCarbonFactorRepository`: a dependency-free implementation of
  `ICarbonFactorRepository` backed by deterministic seeded records.
- `AddCarbonFactorServices()`: DI registration for the in-memory repository and
  `CarbonFactorUseCases`.

`CarbonOps.Api` uses this registration in `Program.cs` and maps the first
minimal HTTP surface for carbon factor lookup:

- `GET /carbon-factors`
- `GET /carbon-factors/{factorId}`
- `GET /carbon-factors/search?category=...&activity=...&region=...&year=...`

Application failures are returned as deterministic HTTP responses using the
shared `ApiError` contract: `not_found` maps to `404`, and `invalid_query`
maps to `400`.

The current endpoint parity coverage is wire-format focused:

- list responses serialize as `factors` plus `total`
- detail responses serialize as `factor`
- factor objects preserve `factor_value` and `factor_unit` snake_case names
- search accepts only `category`, `activity`, `region`, and `year`
- unsupported filters, non-positive years, and non-integer `year` values
  return deterministic `invalid_query` envelopes
- missing factors return deterministic `not_found` envelopes with `details.id`

## Carbon Factor Contract Baseline

The .NET baseline preserves the current Python public field expectations while
using PascalCase C# property names:

- `FactorDto`: `Id`, `Source`, `Category`, `Activity`, `FactorValue`,
  `FactorUnit`, `Region`, `Year`, `Notes`
- `FactorQuery`: `Category`, `Activity`, `Region`, `Year`
- `ApiError`: `Code`, `Message`, `Details`
- `FactorListResponse`: `Factors`, `Total`
- `FactorDetailResponse`: `Factor`

JSON property attributes preserve public names such as `factor_value` and
`factor_unit`. Deterministic helpers are available as `ApiError.NotFound(...)`
and `ApiError.InvalidQuery(...)`.

## Carbon Factor Application Use Cases

`CarbonOps.Application.Factors` contains:

- `ICarbonFactorRepository`: application-owned lookup port returning domain
  `CarbonFactor` records.
- `CarbonFactorUseCases.ListCarbonFactors()`: returns factors sorted by id.
- `CarbonFactorUseCases.GetCarbonFactorById(...)`: returns a detail response or
  deterministic `not_found` error.
- `CarbonFactorUseCases.SearchCarbonFactors(...)`: applies exact-match filters
  for `Category`, `Activity`, `Region`, and `Year`, with deterministic
  `invalid_query` errors for non-positive years or unsupported extra filters.

## Validation

Run these commands from this directory:

```bash
dotnet restore CarbonOps.Api.sln
dotnet build CarbonOps.Api.sln
dotnet test CarbonOps.Api.sln
```
