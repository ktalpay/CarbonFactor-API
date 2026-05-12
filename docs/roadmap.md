# Roadmap

## Current Phase

The current phase establishes independent Python and .NET implementation roots
while preserving existing CarbonOps-API behavior and repository identity.

## Near-Term

1. Keep the current contract, route behavior, transport envelopes, and tests
   stable.
2. Expand documentation around API boundaries, domain model, limitations, and
   production readiness.
3. Define clean-architecture boundaries after establishing implementation
   roots.
4. Prepare a future .NET-first implementation path without changing runtime
   behavior in this task.
5. Keep the Python implementation under `src/python` while preserving current
   behavior.

## Platform Integration Direction

- CarbonOps-Web is the future frontend and platform consumer of CarbonOps-API.
- CarbonOps-Parser remains a separate ingestion and parsing project.
- CarbonOps-API may later consume parser-produced data through an explicit
  artifact, persistence, or service boundary.
- No cross-repository runtime coupling is introduced in this phase.

## Later Phases

- Add the .NET API implementation structure.
- Add persistence behind a documented infrastructure boundary.
- Add authentication and authorization.
- Add audit logging and operational observability.
- Add rate limiting and abuse controls.
- Define parser data ingestion or synchronization contracts.
- Evolve the Python implementation inside `src/python` when explicitly scoped.

## Non-Goals For This Phase

- Production deployment
- External API integrations
- Database infrastructure
- Authentication or authorization
- Audit logging
- Rate limiting
- Parser execution
- Runtime behavior changes
- Official provider compatibility claims
