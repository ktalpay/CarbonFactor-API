# Current API Architecture State

## Current Structure Summary

The repository now contains a small .NET 8 minimal API with focused folders:

- `src/CarbonFactor.Api/Program.cs` configures dependency injection, JSON behavior, Swagger, exception handling, and endpoint mapping.
- `src/CarbonFactor.Api/Endpoints` contains minimal API route definitions.
- `src/CarbonFactor.Api/Contracts` contains request and response DTOs.
- `src/CarbonFactor.Api/Domain` contains carbon factor validation and normalization types.
- `src/CarbonFactor.Api/Errors` contains structured API error DTOs.
- `src/CarbonFactor.Api/Storage` contains the current in-memory storage abstraction and implementation.
- `src/CarbonFactor.Api/Options` contains typed runtime options and validation.
- `tests/CarbonFactor.Api.Tests` contains integration and domain-level tests.
- `docs` contains API, architecture, configuration, and roadmap documentation.

The project currently behaves as a layered minimal API foundation. It is not a full clean architecture implementation, but it separates contracts, domain validation, endpoint routing, storage, configuration, and tests enough to support reviewable extension.

## Key API Responsibilities

- Accept single carbon factor records.
- Accept batch carbon factor records with per-record validation results.
- Normalize supported category and unit variations.
- Query records with filters and pagination.
- Retrieve records by identifier.
- Expose technical dataset summary counts.
- Return consistent structured API errors.
- Expose Swagger/OpenAPI metadata in local development.

## Current Validation Flow

Requests are parsed by ASP.NET Core minimal APIs and validated before storage. Malformed JSON is handled by the exception handler and returned as a structured `invalid_request` error.

Carbon factor domain validation is centralized in `CarbonFactorValidator`. It validates identity, name, category, unit, emission value, and effective-year bounds. Endpoint code maps domain validation errors into API validation errors without duplicating the core rules.

Query validation checks supported normalized category and unit filters plus pagination limits. Pagination defaults and limits are configured through typed options.

## Current Normalization Flow

`CarbonFactorNormalizer` trims names, source, and region values, collapses repeated spaces in names, and normalizes supported category and unit aliases. Examples include `Electricity` to `energy` and `kg CO2e / kWh` to `kg_co2e_per_kwh`.

Unsupported normalized values are rejected with deterministic validation errors.

## Current Persistence and Query Flow

Persistence is currently in memory only through `ICarbonFactorStore` and `InMemoryCarbonFactorStore`. Accepted records are held for the lifetime of the application process and are not durable.

The in-memory store exists to demonstrate API contracts, validation, querying, and workflow boundaries. Durable persistence is intentionally deferred to a future task.

Querying reads from the store, applies optional filters, sorts by name and identifier, and returns bounded pages with total counts. Summary data is computed from the current in-memory records.

## Current Test Coverage Observations

The test suite covers:

- structured error responses
- domain validation and normalization
- single and batch ingestion
- per-record batch validation output
- querying, filtering, pagination, and deterministic ordering
- summary endpoint behavior
- typed configuration validation
- generated Swagger/OpenAPI route coverage

## Risks and Limitations

- Data is in memory only and is lost when the process stops.
- There is no authentication, authorization, rate limiting, or audit trail yet.
- Batch ingestion is synchronous and does not include background jobs.
- CSV/Excel import is not implemented.
- Factor source metadata is intentionally lightweight.
- The summary endpoint returns reporting-support data only and is not a regulatory report.
- The API does not replace legal, accounting, or compliance review.
