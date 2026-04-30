# CarbonFactor API Contract (Pre-Alpha)

## Current Status

- Status: **pre-alpha**
- HTTP server: **not implemented yet**
- Contract style: deterministic in-memory Python models and service behavior

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

## Behavior

- List factors with deterministic sorting by `id`.
- Get factor by exact `id`.
- Search by supported filters (`category`, `activity`, `region`, `year`).
- Return structured error objects for not-found and invalid queries.

## Error Model

`ApiError` includes:
- `code`
- `message`
- `details`

Common codes in this phase:
- `not_found`
- `invalid_query`
- `unsupported_filter`

## Markdown Contract Examples

### List Factors (conceptual)

Request:

```text
list_factors()
```

Response excerpt:

```json
{
  "factors": [
    {"id": "f-001", "category": "electricity"}
  ],
  "total": 3
}
```

### Factor Detail (conceptual)

Request:

```text
get_factor_by_id("f-001")
```

Response excerpt:

```json
{
  "factor": {"id": "f-001", "activity": "grid_consumption"}
}
```

## Limitations

- No HTTP transport layer.
- No persistence/database.
- Uses only synthetic sample data.
- Not production-ready and not a complete emissions data standard implementation.
