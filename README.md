# CarbonOps-API

CarbonOps-API is an open-source climate-tech API project focused on carbon accounting data-platform use cases, specifically emission factors lookup and integration surfaces for sustainability workflows; it is currently implemented as a .NET/ASP.NET Core API foundation (with a separate Python implementation path still present) and is positioned as the API layer that can safely expose curated carbon emissions factor data for applications, services, and future platform integrations.

## Problem Statement

CarbonOps-API is the API layer for carbon factor lookup and platform integration. It exists to provide stable, inspectable API contracts around curated factor records while keeping runtime behavior conservative and pre-alpha.

## Relationship to CarbonOps-Parser

- **CarbonOps-Parser** ingests and normalizes emission factor source data in a separate repository.
- **CarbonOps-API** exposes (and will continue to expand) lookup/integration surfaces over curated platform data.
- This repository does **not** claim production parser/API coupling correctness today.

## Current Status

### Implemented vs. Planned Capabilities

| Area | Implemented in repository | Planned / roadmap |
|---|---|---|
| API host | ASP.NET Core minimal API host (`CarbonOps.Api`) | Additional production hardening and operations controls |
| Carbon factor endpoints | `GET /carbon-factors/`, `GET /carbon-factors/search`, `GET /carbon-factors/{factorId}` | Expanded query/use-case surfaces |
| Operational endpoints | `GET /health`, `GET /health/live`, `GET /health/ready`, `GET /version` | Environment-specific operational checks |
| Application layer | `CarbonFactorUseCases` for list/search/get-by-id | Broader use-case set and integration orchestration |
| Infrastructure adapter | Deterministic in-memory factor repository | Persistence adapters and ingestion paths |
| Contracts | Shared request/response/error contracts in `CarbonOps.Contracts` | Versioning strategy and larger contract set |
| Tests | Domain/application/contracts/infrastructure/API tests in .NET plus Python test suite | Deeper integration and production-readiness test coverage |
| Data posture | Synthetic sample data only | Curated platform data feeds with explicit boundaries |

## Architecture Summary

CarbonOps-API follows a clean architecture direction for a .NET API project:

- **.NET + ASP.NET Core API boundary** (`src/dotnet/src/CarbonOps.Api`)
- **Contracts** (`src/dotnet/src/CarbonOps.Contracts`)
- **Application layer / use cases** (`src/dotnet/src/CarbonOps.Application`)
- **Infrastructure adapters** (`src/dotnet/src/CarbonOps.Infrastructure`)
- **Domain entities** (`src/dotnet/src/CarbonOps.Domain`)
- **Tests** (`src/dotnet/tests/*` and `src/python/tests/*`)

Safe local/dev posture:

- pre-alpha baseline
- synthetic data only
- no production credentials
- no built-in auth, persistence, rate limiting, or compliance claims

## Honest Quickstart (Safe Inspection First)

If you only want to inspect project structure and public API behavior without assuming production readiness:

1. Read architecture and limitations docs:
   - [docs/architecture.md](docs/architecture.md)
   - [docs/limitations.md](docs/limitations.md)
2. Inspect .NET solution and endpoint mapping files:
   - `src/dotnet/CarbonOps.Api.sln`
   - `src/dotnet/src/CarbonOps.Api/Program.cs`
   - `src/dotnet/src/CarbonOps.Api/CarbonFactorEndpoints.cs`
   - `src/dotnet/src/CarbonOps.Api/OperationalEndpoints.cs`
3. Run existing .NET validation commands from `src/dotnet`:

```bash
dotnet restore CarbonOps.Api.sln
dotnet build CarbonOps.Api.sln
dotnet test CarbonOps.Api.sln
```

4. Optional: inspect Python implementation path from `src/python`:

```bash
python --version
python -m pip install -e ".[test]"
python -m pytest -q
```

If your local environment cannot run these commands yet, use the inspection steps above first; do not assume undocumented runtime behavior.

## API Endpoint Summary (Implemented)

### Carbon factor endpoints

- `GET /carbon-factors/`
- `GET /carbon-factors/search`
- `GET /carbon-factors/{factorId}`

Supported search query keys: `category`, `activity`, `region`, `year`, `offset`, `limit`.

### Operational endpoints

- `GET /health`
- `GET /health/live`
- `GET /health/ready`
- `GET /version`

## Public Discoverability Keywords

CarbonOps-API relates to: **carbon accounting**, **emission factors**, **carbon emissions**, **climate-tech**, **.NET**, **ASP.NET Core**, **clean architecture**, **API**, **sustainability**, and **data platform** implementation patterns.

## Suggested GitHub Topics

Repository topics are managed in GitHub settings, not in files. Suggested topics:

- `carbon-accounting`
- `emission-factors`
- `carbon-emissions`
- `climate-tech`
- `dotnet`
- `aspnetcore`
- `clean-architecture`
- `api`
- `sustainability`
- `data-platform`

## Documentation Map

- [Documentation Index](docs/index.md)
- [Architecture](docs/architecture.md)
- [Roadmap](docs/roadmap.md)
- [Quickstart](docs/quickstart.md)
- [Release Notes Draft](docs/release-notes-draft.md)
- [Python Implementation Root](src/python/README.md)
- [.NET Implementation Root](src/dotnet/README.md)
