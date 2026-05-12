# Production Readiness Backlog

CarbonOps-API is not production-ready. The following backlog captures major
areas that must be scoped before production claims are made.

## Security

- authentication
- authorization
- secret management
- dependency vulnerability review
- request size and input hardening

## Operations

- deployment model
- environment configuration
- health and readiness checks
- structured logging
- metrics and tracing
- incident diagnostics

## Reliability

- persistence strategy
- migration strategy
- cache strategy if needed
- retry and timeout policies
- data freshness and lineage checks

## Governance

- API versioning policy
- changelog discipline
- compatibility policy
- public documentation review
- security disclosure process

## Abuse And Safety Controls

- rate limiting
- audit logging
- request throttling
- suspicious traffic monitoring
- safe error messages

None of these items are implemented by API-001.
