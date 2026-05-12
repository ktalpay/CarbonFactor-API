# Clean Architecture

CarbonOps-API is intended to evolve toward clean architecture with a .NET-first
implementation path and a future independent Python path.

## Target Layering

```text
API
  -> Application
  -> Domain
  -> Infrastructure
```

## Current Python Slice

The current Python implementation now exposes a first structural slice under
`src/python/src/carbonops_api`:

- `api/`: API boundary wrapper for the current HTTP adapter
- `application/`: factor lookup use cases
- `contracts/`: DTOs, query models, response models, and error helpers
- `domain/`: CarbonFactor entity concepts
- `infrastructure/`: synthetic in-memory factor data
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

The current FastAPI adapter is local-only and preserved for existing tests. A
future .NET API layer should provide the primary platform implementation.

## Application Layer

The application layer should express use cases without depending on a specific
web framework or persistence provider:

- list carbon factors
- get a carbon factor by identifier
- search carbon factors by supported filters
- validate request intent before infrastructure access
- coordinate future parser-fed or persisted data reads

The current Python slice now introduces an application repository port for
factor lookup so use cases do not read synthetic infrastructure data directly.

## Domain Layer

The domain layer owns CarbonFactor concepts and validation rules. It should not
know about HTTP, FastAPI, ASP.NET, PostgreSQL, files, or external source
systems.

In the current Python slice, `carbonops_api.domain` contains the factor entity
type and does not import FastAPI, transport, or infrastructure modules.

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
synthetic sample data. Transport and compatibility wiring use that adapter to
preserve current behavior.

## Implementation Path Rule

When .NET and Python paths are added, they should be independent
implementations of the same conceptual API and domain model. They should not
import from each other or require each other at runtime.
