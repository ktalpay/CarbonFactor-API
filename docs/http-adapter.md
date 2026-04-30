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
