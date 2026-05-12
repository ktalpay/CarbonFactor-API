# Roadmap

## Current Phase

API-001 establishes the CarbonOps-API documentation baseline and repository
identity. It reframes the project as the CarbonOps platform API reference while
preserving existing behavior.

## Near-Term

1. Keep the current contract, route behavior, transport envelopes, and tests
   stable.
2. Expand documentation around API boundaries, domain model, limitations, and
   production readiness.
3. Define clean-architecture boundaries before moving implementation files.
4. Prepare a future .NET-first implementation path without changing runtime
   behavior in this task.
5. Prepare a future independent Python implementation path without adding Python
   implementation code in this task.

## Platform Integration Direction

- CarbonOps-Web is the future frontend and platform consumer of CarbonOps-API.
- CarbonOps-Parser remains a separate ingestion and parsing project.
- CarbonOps-API may later consume parser-produced data through an explicit
  artifact, persistence, or service boundary.
- No cross-repository runtime coupling is introduced in API-001.

## Later Phases

- Add the .NET API implementation structure.
- Add persistence behind a documented infrastructure boundary.
- Add authentication and authorization.
- Add audit logging and operational observability.
- Add rate limiting and abuse controls.
- Define parser data ingestion or synchronization contracts.
- Add an independent Python implementation path when explicitly scoped.

## Non-Goals For API-001

- Production deployment
- External API integrations
- Database infrastructure
- Authentication or authorization
- Audit logging
- Rate limiting
- Parser execution
- Runtime behavior changes
- Official provider compatibility claims
