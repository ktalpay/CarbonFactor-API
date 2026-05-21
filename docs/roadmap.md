# Roadmap

## Current Phase

The current phase focuses on public documentation baseline quality, discoverability, and architecture clarity for CarbonOps-API as a clean architecture .NET/ASP.NET Core API project for carbon accounting and emission factors workflows.

## Near-Term

1. Keep implemented route behavior and tests stable while improving documentation quality.
2. Expand API boundary, limitations, and production-readiness documentation with evidence-backed statements only.
3. Continue clean architecture layering across contracts, application, infrastructure adapters, and tests.
4. Expand the .NET-first implementation path without changing public runtime behavior unless explicitly scoped.
5. Keep the Python implementation under `src/python` as an independent path while maintaining current behavior.

## Platform Integration Direction

- CarbonOps-Web is a future consumer surface for CarbonOps-API.
- CarbonOps-Parser remains a separate ingestion/normalization project for source data.
- CarbonOps-API may later expose curated parser-fed data through explicit artifact or persistence boundaries.
- No cross-repository runtime coupling is introduced in this phase.

## Later Phases

- Add broader .NET carbon factor use cases behind existing boundaries.
- Add persistence adapters behind infrastructure contracts.
- Add authentication and authorization capabilities.
- Add audit logging and operational observability.
- Add rate limiting and abuse controls.
- Define parser data ingestion/synchronization contracts.
- Evolve Python implementation only when explicitly scoped.

## Non-Goals For Current Phase

- Production deployment claims
- Legal/compliance correctness claims
- Official source-owner correctness claims
- Database infrastructure rollout
- Authentication or authorization rollout
- Audit logging rollout
- Rate limiting rollout
- Parser execution inside this API runtime
- Runtime behavior changes without explicit scope
