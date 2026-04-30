# CarbonFactor API Architecture (Current)

## Scope

This repository currently establishes a deterministic contract foundation for carbon factor lookup behavior. It avoids web/database/runtime infrastructure.

## Components

- `contracts.py`: dataclass DTO and response/error shapes.
- `errors.py`: deterministic helper constructors for API-like errors.
- `sample_data.py`: synthetic in-memory factors.
- `catalog.py`: list/get/search behavior over sample data.
- `tests/`: behavior checks for contracts, errors, and catalog flow.

## Future Direction (Conservative)

A future run can add an HTTP server adapter after contracts stabilize. That adapter should map transport concerns onto the already-tested contract/service layer.

## Transport Boundary Layer

A new `transport/` package now sits above catalog/services and provides envelope objects, status mapping, serialization helpers, and local handler functions. This layer is intentionally framework-agnostic so a future HTTP adapter can call into it without changing core contract behavior.

## FastAPI adapter layer

A minimal FastAPI layer sits above transport handlers for local HTTP parity tests. Business logic remains in catalog and transport modules.


## HTTP boundary notes

Route functions stay thin and delegate to transport handlers. Query parsing/validation helpers in the HTTP layer only shape supported query keys and normalize unknown query parameters into deterministic transport errors.
