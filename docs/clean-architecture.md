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

## Domain Layer

The domain layer owns CarbonFactor concepts and validation rules. It should not
know about HTTP, FastAPI, ASP.NET, PostgreSQL, files, or external source
systems.

## Infrastructure Layer

The infrastructure layer should contain implementation details that can change
without rewriting domain behavior:

- database access
- parser-produced data ingestion boundaries
- cache providers
- external service clients
- operational logging and metrics sinks

None of these infrastructure features are added in API-001.

## Implementation Path Rule

When .NET and Python paths are added, they should be independent
implementations of the same conceptual API and domain model. They should not
import from each other or require each other at runtime.
