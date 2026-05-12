# CarbonOps-API

CarbonOps-API is the backend API reference project for the CarbonOps platform.
It is documented as a .NET-first API foundation that will evolve toward clean
architecture while preserving a future independent Python implementation path,
similar to the CarbonOps-Parser repository model.

The repository was previously framed as CarbonFactor API. In this repository,
`CarbonFactor` remains valid domain wording when it refers to a carbon factor
record or related entity. Product and repository references should use
CarbonOps-API.

## Current Status

CarbonOps-API is in documentation-baseline status with independent Python and
.NET implementation roots established.

- Existing runtime behavior is preserved.
- Existing endpoint documentation is preserved.
- The current local adapter remains pre-alpha and synthetic-data-only.
- The active Python implementation now lives under `src/python`.
- The initial .NET Clean Architecture solution skeleton now exists under
  `src/dotnet`.
- No authentication, database persistence, audit logging, rate limiting, parser
  execution, or production deployment behavior is included.

## Phase 1 Scope

Phase 1 establishes the public documentation baseline for CarbonOps-API:

1. Present the repository as the CarbonOps platform API project.
2. Document API boundaries and current limitations.
3. Preserve the existing carbon factor lookup contract.
4. Define the future clean-architecture direction.
5. Define planned .NET and Python implementation options.
6. Clarify how CarbonOps-API relates to CarbonOps-Parser and CarbonOps-Web.

## Architecture At A Glance

```text
CarbonOps-Web
  -> CarbonOps-API
  -> API boundary and transport contract
  -> CarbonFactor domain model
  -> data access boundary
  -> future parser-fed or persisted factor records

CarbonOps-Parser
  -> produces validated carbon factor source data in a separate project
  -> may provide future input artifacts or persistence feeds
```

The current checked-in implementation remains a deterministic local contract
foundation with synthetic sample records, transport envelopes, a thin FastAPI
adapter for local tests, and behavior tests.

## Repository Layout

- `src/python`: current Python implementation root
- `src/python/src/carbonops_api`: Python package source
- `src/python/tests`: Python test suite
- `src/dotnet`: .NET 8 Clean Architecture solution skeleton

## Implementation Options

### .NET

.NET is the intended primary implementation direction for the platform API. The
.NET path now has an initial Clean Architecture solution skeleton with separated
API, application, domain, contracts, infrastructure, and test projects.

This phase does not add carbon factor CRUD behavior, persistence, authentication,
or production runtime features. The .NET API project includes only minimal host
startup needed for the skeleton to build and test.

### Python

Python is planned as an independent implementation option for users and
contributors who prefer Python-oriented API or data workflows. It should follow
the same public API concepts and domain semantics as the .NET path without
depending on the .NET implementation.

The current Python implementation root is available at `src/python`.

## API Boundary

CarbonOps-API owns the backend API boundary for carbon factor lookup and related
platform workflows.

Current local routes are unchanged:

- `GET /health`
- `GET /factors`
- `GET /factors/{factor_id}`

`GET /factors` supports only these query parameters:

- `category`
- `activity`
- `region`
- `year`

Unsupported query keys return a deterministic transport error envelope with
HTTP 400 and `invalid_query`. Unknown framework routes remain FastAPI-native
404 responses and are intentionally not wrapped by transport envelopes.

## Domain Model Summary

The current domain surface centers on `CarbonFactor`-style records:

- `id`
- `source`
- `category`
- `activity`
- `factor_value`
- `factor_unit`
- `region`
- `year`
- `notes`

The current repository uses synthetic sample data only. CarbonOps-Parser may
later provide validated source data, but parser execution and cross-repository
runtime coupling are out of scope for this phase.

## Documentation Map

- [Documentation Index](docs/index.md)
- [Python Implementation Root](src/python/README.md)
- [.NET Implementation Root](src/dotnet/README.md)
- [Architecture](docs/architecture.md)
- [Clean Architecture](docs/clean-architecture.md)
- [Clean Code Guidelines](docs/clean-code-guidelines.md)
- [API Boundaries](docs/api-boundaries.md)
- [Domain Model](docs/domain-model.md)
- [Implementation Options](docs/implementation-options.md)
- [Parity Model](docs/parity-model.md)
- [API Contract](docs/api-contract.md)
- [HTTP Adapter](docs/http-adapter.md)
- [Transport Boundary](docs/transport-boundary.md)
- [Production Readiness Backlog](docs/production-readiness-backlog.md)
- [Roadmap](docs/roadmap.md)
- [Limitations](docs/limitations.md)
- [Public Safety](docs/public-safety.md)

## Roadmap Summary

- Complete the documentation baseline.
- Keep current API behavior stable while contract documentation matures.
- Define clean-architecture target boundaries before moving implementation
  files.
- Establish independent Python and .NET implementation roots.
- Expand the .NET skeleton into the .NET-first implementation path in later
  tasks.
- Define safe integration points with CarbonOps-Parser and CarbonOps-Web.
- Defer production hardening until explicit production-readiness tasks.

## Governance

Documentation and implementation work should preserve these guardrails:

- Do not overstate production readiness.
- Keep CarbonOps-API independent from CarbonOps-Parser and CarbonOps-Web at
  runtime unless a later task explicitly adds an integration.
- Treat `CarbonFactor` as domain terminology, not the repository name.
- Preserve endpoint behavior unless a task explicitly changes the public
  contract.
- Keep .NET and Python implementation paths independent when they are added.

## Non-Goals

This phase does not:

- Rename the `carbonops_api` package namespace.
- Add Python implementation code.
- Add authentication.
- Add database persistence.
- Add audit logging.
- Add rate limiting.
- Add parser execution.
- Change API runtime behavior.
- Remove existing tests.
