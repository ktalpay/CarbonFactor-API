# CarbonFactor API

CarbonFactor API is currently in **pre-alpha** status.

## Current Implementation

This repository currently provides a deterministic, in-memory API contract foundation for carbon factor lookup behavior.

- No HTTP server is implemented yet.
- No database dependency is included.
- Only synthetic sample factor data is included.

## Included Modules

- Contract DTOs and response models
- Deterministic error helpers
- In-memory factor catalog functions (list/get/search)
- Unit tests for behavior verification

See:
- `docs/api-contract.md`
- `docs/architecture.md`

## Transport Boundary

A local transport contract layer now exists under `carbonfactor_api.transport` with:

- deterministic response envelopes (`ResponseEnvelope`, `ErrorEnvelope`)
- HTTP-style status mapping helpers
- serialization helpers for contract DTOs and responses
- local handler functions for list/detail behaviors

This remains framework-free and does **not** start a real HTTP server.

See:
- `docs/transport-boundary.md`
- `docs/api-contract.md`

## Local HTTP adapter

A thin FastAPI adapter is available for local testing only. It delegates route behavior to existing transport handlers and does not add deployment, auth, database, or external provider integrations. See `docs/http-adapter.md`.
