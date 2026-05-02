# Carbon Factor Querying

Carbon factors can be queried with optional filters and bounded pagination.

## Endpoint

`GET /api/carbon-factors`

## Supported Filters

| Query parameter | Behavior |
| --- | --- |
| `category` | Matches normalized category values such as `energy`, `transport`, or common aliases such as `Electricity`. |
| `unit` | Matches normalized units such as `kg_co2e`, `kg_co2e_per_kwh`, or common textual variations. |
| `source` | Case-insensitive exact match against source text. |
| `region` | Case-insensitive exact match against region text. |
| `effectiveYear` | Exact match against the effective year. |
| `search` | Case-insensitive contains search over factor names. |

Results are sorted by name and then identifier for deterministic output.

## Pagination

| Query parameter | Default | Limit |
| --- | --- | --- |
| `page` | `1` | Must be greater than `0`. |
| `pageSize` | `25` | Must be greater than `0` and less than or equal to `100`. |

Response:

```json
{
  "page": 1,
  "pageSize": 25,
  "totalCount": 1,
  "totalPages": 1,
  "items": [
    {
      "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "name": "Grid electricity",
      "category": "energy",
      "unit": "kg_co2e_per_kwh",
      "emissionValue": 0.42,
      "source": "Example source",
      "region": "GB",
      "effectiveYear": 2025
    }
  ]
}
```

Empty result sets return `200 OK` with `totalCount` and `totalPages` set to `0`.

## Examples

Filter by category:

```http
GET /api/carbon-factors?category=energy
```

Search and paginate:

```http
GET /api/carbon-factors?search=electricity&page=1&pageSize=10
```

## Known Limitations

- Querying uses the current in-memory store.
- Filters are intentionally simple exact matches except for factor-name search.
- Sorting is deterministic but not configurable.

