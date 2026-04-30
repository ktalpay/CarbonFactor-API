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
