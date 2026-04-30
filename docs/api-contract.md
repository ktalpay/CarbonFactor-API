# CarbonFactor API Contract (Pre-Alpha)

## Current Status

- Status: **pre-alpha**
- HTTP server: **not implemented yet**
- Contract style: deterministic in-memory Python models and service behavior
- Transport boundary: deterministic local envelope + status mapping layer (no framework)

## DTO Models

`FactorDto` fields:
- `id`
- `source`
- `category`
- `activity`
- `factor_value`
- `factor_unit`
- `region` (optional)
- `year` (optional)
- `notes` (optional)

`FactorQuery` filter fields:
- `category`
- `activity`
- `region`
- `year`

## Transport Contract Layer

Envelope shape:
- `status`
- `data`
- `error`

Error shape:
- `code`
- `message`
- `details`

Status mappings:
- `200` success
- `400` invalid query / unsupported filter
- `404` not found
- `422` validation-style error (reserved)

## Behavior

- List factors with deterministic sorting by `id`.
- Get factor by exact `id`.
- Search by supported filters (`category`, `activity`, `region`, `year`).
- Return structured error objects for not-found and invalid queries.
- Serialize responses through deterministic transport helpers.

## Local Handler Entry Points

- `handle_list_factors(query: dict)`
- `handle_get_factor(factor_id: str)`

These are local functions only and are intended to be adapted into a future HTTP framework.

## Limitations

- No HTTP transport server.
- No persistence/database.
- Uses only synthetic sample data.
- Not production-ready and not a complete emissions data standard implementation.
