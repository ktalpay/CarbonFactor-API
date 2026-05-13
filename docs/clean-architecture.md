# Clean Architecture

CarbonOps-API is intended to evolve toward clean architecture with a .NET-first
implementation path and an independent Python path.

## Target Layering

```text
API
  -> Application
  -> Domain
  -> Infrastructure
```

## Current .NET Skeleton

The .NET implementation root at `src/dotnet` now contains a .NET 8 solution
skeleton:

- `CarbonOps.Domain`: domain-only project
- `CarbonOps.Application`: use-case project
- `CarbonOps.Contracts`: public contract project
- `CarbonOps.Infrastructure`: adapter project
- `CarbonOps.Api`: ASP.NET Core composition root
- `tests/*`: smoke and lightweight dependency-boundary tests

The skeleton does not implement carbon factor CRUD behavior yet. It now includes
a first .NET carbon factor domain model, API-facing contract DTO baseline, and
application-layer carbon factor lookup use cases so contract parity work can
proceed without adding HTTP routes or persistence.

## Current Python Slice

The current Python implementation now exposes a first structural slice under
`src/python/src/carbonops_api`:

- `api/`: API boundary wrapper for the current HTTP adapter
- `application/`: factor lookup use cases
- `contracts/`: DTOs, query models, response models, and error helpers
- `domain/`: CarbonFactor entity concepts
- `infrastructure/`: synthetic in-memory factor data
- `composition.py`: default application wiring for the in-memory implementation
- `http/`: existing FastAPI adapter preserved for compatibility
- `transport/`: framework-neutral transport envelopes and handlers

Top-level compatibility modules remain in place for `catalog`, `errors`, and
`sample_data` so existing imports keep working during the transition.

## API Layer

The API layer owns transport-specific behavior:

- route definitions
- request parsing
- response status mapping
- OpenAPI surface
- authentication and authorization when later added
- API versioning when later added

The current FastAPI adapter is local-only and preserved for existing tests. The
current .NET API project now includes a minimal host, health endpoint, and the
first carbon factor lookup routes for list, get-by-id, and search behavior.

## Application Layer

The application layer should express use cases without depending on a specific
web framework or persistence provider:

- list carbon factors
- get a carbon factor by identifier
- search carbon factors by supported filters
- validate request intent before infrastructure access
- coordinate future parser-fed or persisted data reads

The current Python slice now includes an application repository port for factor
lookup so use cases do not read synthetic infrastructure data directly. The
current .NET slice mirrors that boundary with `ICarbonFactorRepository` in
`CarbonOps.Application`; its list, get-by-id, and search use cases depend only
on Domain and Contracts.

## Domain Layer

The domain layer owns CarbonFactor concepts and validation rules. It should not
know about HTTP, FastAPI, ASP.NET, PostgreSQL, files, or external source
systems.

In the current .NET slice, `CarbonOps.Domain` contains `CarbonFactor` as the
domain concept for a carbon factor record. It preserves the current contract
field semantics with PascalCase C# members and required text validation for
core fields.

In the current Python slice, `carbonops_api.domain` contains the factor entity
type and does not import FastAPI, transport, or infrastructure modules.

## Contract Layer

The contract layer owns API-facing DTO shapes and deterministic error models.
In the current .NET slice, `CarbonOps.Contracts` contains `FactorDto`,
`FactorQuery`, `ApiError`, `FactorListResponse`, and `FactorDetailResponse`.
These models do not depend on Infrastructure or API projects, and JSON field
names document parity with the current Python public contract.

## Infrastructure Layer

The infrastructure layer should contain implementation details that can change
without rewriting domain behavior:

- database access
- in-memory repository adapters
- parser-produced data ingestion boundaries
- cache providers
- external service clients
- operational logging and metrics sinks

None of these infrastructure features are added in this phase.

The current Python slice includes an in-memory repository adapter backed by
synthetic sample data. Composition owns the default repository wiring, and
transport plus compatibility facades use composition to preserve current
behavior.

## Implementation Path Rule

The .NET and Python paths are independent implementations of the same
conceptual API and domain model. They should not import from each other or
require each other at runtime.
