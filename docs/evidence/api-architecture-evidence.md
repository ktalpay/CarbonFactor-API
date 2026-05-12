# API Architecture Evidence Summary (Pre-Alpha)

This document summarizes current CarbonOps-API behavior implemented in this
repository for local development with synthetic data.

## End-to-end flow

```text
HTTP request
  -> FastAPI adapter
  -> HTTP query validation
  -> transport handler
  -> catalog service
  -> DTO/serialization
  -> response envelope
```

## Modules and responsibilities

- **FastAPI adapter (`src/python/src/carbonops_api/http/app.py`)**
  - Exposes `GET /health`, `GET /factors`, and `GET /factors/{factor_id}`.
  - Translates HTTP inputs into transport-layer calls.
  - Applies explicit supported-query validation for `GET /factors`.

- **HTTP query validation (`src/python/src/carbonops_api/http/app.py`)**
  - Allows only `category`, `activity`, `region`, and `year`.
  - Produces deterministic `invalid_query` transport envelopes for unsupported keys.

- **Transport boundary (`src/python/src/carbonops_api/transport/`)**
  - Defines response and error envelopes.
  - Maps local outcomes to deterministic HTTP-style status codes.
  - Serializes contract objects into envelope-compatible payloads.

- **Catalog service (`src/python/src/carbonops_api/catalog.py`)**
  - Performs deterministic list/detail/filter operations over synthetic in-memory factors.
  - Does not depend on external infrastructure.

- **Contract DTOs and errors (`src/python/src/carbonops_api/contracts.py`, `src/python/src/carbonops_api/errors.py`)**
  - Define factor/query structures and deterministic error semantics used across layers.

## Contract boundaries

- **Adapter boundary:** HTTP semantics are intentionally thin and delegated.
- **Transport boundary:** framework-neutral envelope/status behavior is centralized.
- **Catalog boundary:** data access remains in-memory and deterministic.
- **Contract boundary:** DTO/error shapes constrain the data and failure semantics exposed upward.

## What is currently tested

- HTTP route behavior parity for list/detail flows.
- Unsupported query key behavior for deterministic `invalid_query` responses.
- OpenAPI metadata and route/query-parameter visibility.
- Transport envelope serialization and status mapping consistency.
- Not-found and invalid-path consistency at the HTTP boundary.

## What is intentionally not implemented

- Production deployment/runtime infrastructure.
- Database/persistence behavior.
- Authentication/authorization/rate limiting.
- Cloud/container orchestration specifics.
- External provider integrations.
- Cross-repository runtime coupling to CarbonOps-Parser or CarbonOps-Web.

## Why this remains pre-alpha

- Behavior is validated primarily for local deterministic development.
- Data is synthetic and in-memory.
- Operational hardening, governance, and integration maturity are intentionally deferred.
- Current evidence shows implementation clarity and testability, not production readiness.

## Future extension points (conservative)

- Introduce parser-fed local data artifacts behind existing catalog seams.
- Expand query/error validation test matrices without changing current API surface.
- Add optional schema/versioning discipline for contract evolution.
- Continue keeping adapter thin while transport and contract layers stabilize.
