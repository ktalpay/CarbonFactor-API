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
- Test projects provide smoke, boundary, and focused model contract tests.
- No carbon factor CRUD behavior, persistence, authentication, authorization,
  parser execution, background jobs, or production hardening is implemented.
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
  contracts, but not infrastructure or API.
- `CarbonOps.Contracts`: public contract models; no infrastructure or API
  dependency.
- `CarbonOps.Infrastructure`: adapter implementations when added; must not
  reference the API project.
- `CarbonOps.Api`: composition root and ASP.NET Core host.

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

## Validation

Run these commands from this directory:

```bash
dotnet restore CarbonOps.Api.sln
dotnet build CarbonOps.Api.sln
dotnet test CarbonOps.Api.sln
```
