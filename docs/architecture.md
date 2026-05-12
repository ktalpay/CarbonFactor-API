# Architecture

CarbonOps-API is the backend API reference project for the CarbonOps platform.
It owns the public API boundary for carbon factor lookup and future platform API
workflows.

## Current Repository Shape

The current checked-in implementation is a pre-alpha local contract foundation:

- contract DTOs and response/error shapes
- deterministic error helpers
- synthetic in-memory factor records
- catalog list/get/search behavior
- framework-neutral transport envelopes and status mapping
- a thin FastAPI adapter for local contract testing
- behavior tests for current API semantics

The current Python implementation is organized under:

- `src/python/src/carbonops_api`
- `src/python/tests`

Within the Python package, the current transitional architecture is:

- `domain`: factor entity concepts
- `application`: list/get/search use cases and repository ports
- `contracts`: public DTOs and response/query models
- `infrastructure`: synthetic in-memory factor data and in-memory repository adapters
- `composition.py`: default wiring between application ports and infrastructure adapters
- `api`: API boundary wrapper
- `http`: current FastAPI adapter
- `transport`: framework-neutral transport behavior

Compatibility modules remain at the top level for `catalog`, `errors`, and
`sample_data`.

The planned .NET implementation root exists at `src/dotnet`.

## Platform Relationship

```text
CarbonOps-Web
  -> calls CarbonOps-API for platform backend behavior

CarbonOps-API
  -> exposes carbon factor API contracts
  -> owns API boundary, transport behavior, and domain-facing responses

CarbonOps-Parser
  -> remains a separate ingestion and parsing project
  -> may later provide validated factor data through files, storage, or another
     explicit integration boundary
```

There is no runtime dependency between these repositories in this phase.

## Conceptual Flow

```text
HTTP request
  -> API adapter
  -> query validation
  -> transport handler
  -> composition
  -> application use case
  -> repository port
  -> in-memory repository adapter
  -> response envelope
```

The current implementation expresses this flow with local Python modules and
synthetic data. The target architecture should later express the same concerns
through a .NET-first clean-architecture implementation, with an independent
Python implementation option.

## Target Boundaries

- **API boundary:** routes, request parsing, OpenAPI surface, response status
  behavior.
- **Application boundary:** use cases such as list, detail, and search carbon
  factors.
- **Domain boundary:** CarbonFactor records and domain validation concepts.
- **Infrastructure boundary:** persistence, external stores, parser-fed data,
  and deployment concerns.
- **Integration boundary:** explicit contracts for CarbonOps-Parser outputs and
  CarbonOps-Web consumption.

## Current Limitations

CarbonOps-API currently uses synthetic in-memory data and local test adapters.
It does not include authentication, database persistence, parser execution,
audit logging, rate limiting, or production deployment infrastructure.
