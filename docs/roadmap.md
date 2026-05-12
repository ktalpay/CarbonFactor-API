# Roadmap

## Current Phase

The current phase establishes independent Python and .NET implementation roots
while preserving existing CarbonOps-API behavior and repository identity.

## Near-Term

1. Keep the current contract, route behavior, transport envelopes, and tests
   stable.
2. Expand documentation around API boundaries, domain model, limitations, and
   production readiness.
3. Keep clean-architecture boundaries explicit as implementation work expands.
4. Expand the .NET-first implementation path without changing existing runtime
   behavior unless explicitly scoped.
5. Keep the Python implementation under `src/python` while preserving current
   behavior.

## Platform Integration Direction

- CarbonOps-Web is the future frontend and platform consumer of CarbonOps-API.
- CarbonOps-Parser remains a separate ingestion and parsing project.
- CarbonOps-API may later consume parser-produced data through an explicit
  artifact, persistence, or service boundary.
- No cross-repository runtime coupling is introduced in this phase.

## Later Phases

- Add .NET carbon factor use cases and API behavior behind the established
  project boundaries.
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
