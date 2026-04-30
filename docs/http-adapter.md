# HTTP Adapter (FastAPI, local-only)

The project includes a thin FastAPI adapter for local development and testing.

## Scope

- Adapter-only HTTP wiring.
- Core logic remains in contracts, catalog service, and transport handlers.
- No deployment/runtime hosting guidance is included.

## Routes

- `GET /health`
- `GET /factors`
- `GET /factors/{factor_id}`

## Notes

- Routes delegate to transport handlers and return deterministic envelopes.
- No database, authentication, or external provider integrations are implemented.
- Production hardening is intentionally out of scope at this stage.


## Query behavior

`GET /factors` supports: `category`, `activity`, `region`, `year`.

- Unsupported query params return transport-envelope `invalid_query` (HTTP 400) with deterministic `details.unsupported_query_keys`.
- `year` remains typed as integer at the adapter boundary.
- Unknown paths are framework-level FastAPI 404 responses and are not envelope-wrapped.

## OpenAPI checks

Tests validate deterministic title/version and route/query-parameter visibility in generated OpenAPI, without committing generated spec artifacts.
