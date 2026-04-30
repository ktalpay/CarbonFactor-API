# Technical Evidence Index (Pre-Alpha)

This index consolidates current, local-only technical evidence for CarbonFactor API. It is limited to deterministic behavior implemented in this repository with synthetic data.

## API contract foundation
- **Implemented artifacts:** `src/carbonfactor_api/contracts.py`, `docs/api-contract.md`.
- **Why this matters:** establishes stable DTO and query/filter shapes for deterministic factor lookup behavior.
- **Evidence relevance (conservative):** demonstrates baseline contract clarity for local validation and testability.
- **Current limitation:** no versioned external schema distribution.
- **Next improvement candidate:** add explicit contract version markers and changelog discipline.

## Error model
- **Implemented artifacts:** `src/carbonfactor_api/errors.py`, transport envelopes in `src/carbonfactor_api/transport/envelope.py`.
- **Why this matters:** ensures deterministic error codes/messages/details across local contract and HTTP adapter behavior.
- **Evidence relevance (conservative):** supports reproducible negative-path verification.
- **Current limitation:** does not include internationalization or policy-based error classification.
- **Next improvement candidate:** expand error-code matrix documentation and test matrix coverage.

## In-memory catalog service
- **Implemented artifacts:** `src/carbonfactor_api/catalog.py`, synthetic records in `src/carbonfactor_api/sample_data.py`.
- **Why this matters:** isolates deterministic list/get/search behavior independent of infrastructure.
- **Evidence relevance (conservative):** shows local lookup semantics with synthetic data only.
- **Current limitation:** no persistence or lineage for real emissions datasets.
- **Next improvement candidate:** define seam for parser-produced local artifacts without changing API behavior.

## Transport boundary
- **Implemented artifacts:** `src/carbonfactor_api/transport/`, `docs/transport-boundary.md`.
- **Why this matters:** keeps framework-neutral response envelope/status mapping logic separate from HTTP framework concerns.
- **Evidence relevance (conservative):** demonstrates local contract-to-response normalization prior to broader integration work.
- **Current limitation:** no async/event transport variants and no protocol-level interoperability guarantees.
- **Next improvement candidate:** add expanded envelope validation tests for edge-case serialization.

## FastAPI adapter
- **Implemented artifacts:** `src/carbonfactor_api/http/app.py`, `docs/http-adapter.md`.
- **Why this matters:** provides thin route wiring to validate transport behavior over HTTP semantics in local development.
- **Evidence relevance (conservative):** confirms adapter parity with transport handlers for current routes.
- **Current limitation:** no deployment/auth/rate-limit/production runtime posture.
- **Next improvement candidate:** strengthen adapter-level negative-path assertions while retaining thin adapter scope.

## HTTP query validation
- **Implemented artifacts:** query validation in `src/carbonfactor_api/http/query.py` and `src/carbonfactor_api/http/routes.py`; tests in `tests/test_http_query.py`.
- **Why this matters:** enforces deterministic supported query keys and predictable invalid-query envelopes.
- **Evidence relevance (conservative):** provides repeatable local evidence for boundary input handling.
- **Current limitation:** validation scope intentionally narrow to current route/query set.
- **Next improvement candidate:** add case/whitespace and mixed-invalid-key scenario tests.

## OpenAPI inspection tests
- **Implemented artifacts:** `tests/test_http_openapi.py`.
- **Why this matters:** verifies deterministic metadata and route/query visibility for the generated local schema.
- **Evidence relevance (conservative):** supports local reviewer confidence in documented HTTP surface.
- **Current limitation:** no published or versioned OpenAPI artifact in-repo.
- **Next improvement candidate:** document lightweight process for pinned OpenAPI snapshots when contract stabilizes.

## HTTP error consistency tests
- **Implemented artifacts:** `tests/test_http_routes.py`, `tests/test_http_integration.py`, and transport tests (`tests/test_transport_envelope.py`, `tests/test_transport_status.py`, `tests/test_transport_serialization.py`, `tests/test_transport_handlers.py`).
- **Why this matters:** validates envelope consistency for invalid query and not-found flows.
- **Evidence relevance (conservative):** demonstrates deterministic failure behavior in local environment.
- **Current limitation:** no broader fuzz/property-style coverage.
- **Next improvement candidate:** add parameterized negative-path test expansion.

## CI and test coverage
- **Implemented artifacts:** `.github/workflows/test.yml`, `python -m pytest -q` test suite.
- **Why this matters:** enforces repeatable local test execution for implemented pre-alpha behavior.
- **Evidence relevance (conservative):** indicates baseline verification automation, not production quality guarantees.
- **Current limitation:** no deployment/security/compliance pipelines.
- **Next improvement candidate:** include stricter lint/type checks if they remain lightweight and local.

## Limitations and non-goals
- **Implemented references:** `README.md`, `docs/roadmap.md`, `docs/evidence-backlog.md`.
- **Why this matters:** constrains claims to pre-alpha, local development, synthetic data.
- **Evidence relevance (conservative):** reduces risk of overstating maturity or external compatibility.
- **Current limitation:** documentation-only guardrails rely on ongoing discipline.
- **Next improvement candidate:** add reviewer checklist for claim language in future docs updates.
