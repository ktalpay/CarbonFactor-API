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
