# Domain Model

The current CarbonOps-API domain model centers on carbon factor records.
`CarbonFactor` is still valid wording when it refers to the domain entity or
concept.

## CarbonFactor Record

Current fields:

- `id`
- `source`
- `category`
- `activity`
- `factor_value`
- `factor_unit`
- `region`
- `year`
- `notes`

## Query Model

Current supported filters:

- `category`
- `activity`
- `region`
- `year`

## Error Model

Current deterministic error behavior includes:

- not-found responses for missing factor identifiers
- invalid-query responses for unsupported filter keys
- reserved validation-style status mapping for future validation cases

## Data Source Status

The current repository uses synthetic in-memory records. These records are for
contract and behavior testing only.

CarbonOps-Parser may later provide validated source data through an explicit
handoff boundary, but parser execution and real source ingestion are outside the
current API runtime.

## Domain Boundaries

The domain model should not depend on:

- HTTP frameworks
- database drivers
- CarbonOps-Web UI concepts
- CarbonOps-Parser runtime code
- source-owner document formats
