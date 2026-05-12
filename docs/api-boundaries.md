# API Boundaries

CarbonOps-API owns the backend API boundary for CarbonOps platform workflows
that need carbon factor lookup behavior.

## Current Boundary

The current local HTTP surface is:

- `GET /health`
- `GET /factors`
- `GET /factors/{factor_id}`

The current supported query parameters for `GET /factors` are:

- `category`
- `activity`
- `region`
- `year`

Unsupported query parameters return a deterministic `invalid_query` transport
envelope. Unknown framework routes remain framework-level 404 responses.

## Owned By CarbonOps-API

- API route contracts
- request validation at the API boundary
- response envelope and status behavior
- CarbonFactor lookup behavior
- future authentication and authorization boundaries when scoped
- future persistence-facing API behavior when scoped

## Not Owned By CarbonOps-API

- source document acquisition
- source-specific parsing
- source-owner data correctness
- frontend rendering and user workflows
- production reporting or certification

CarbonOps-Parser owns source ingestion and parsing. CarbonOps-Web owns frontend
experience and user interaction. This phase does not add runtime integration with
either project.

## Future Boundary Questions

Later tasks should decide:

- whether public routes should be versioned
- how persisted factor records are queried
- how parser-produced data is loaded or synchronized
- how authentication, authorization, audit logging, and rate limiting are
  introduced
- how .NET and Python implementations demonstrate contract parity
