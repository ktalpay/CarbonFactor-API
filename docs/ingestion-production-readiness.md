# Ingestion production readiness review (ING-007)

Date: 2026-05-21  
Scope: `src/dotnet` ingestion boundary contract, endpoint, validation service, tests, and related contract documentation.

## Verdict

Current ingestion flow is **not production ready**. It is **ready for controlled integration and security hardening** ahead of SEC-001/SEC-002 and PT-002.

## Current implemented ingestion boundary capabilities

- Parser-output DTO contract exists in `CarbonOps.Contracts`:
  - `ParserCarbonFactorBatchImportRequest`
  - `ParserSourceMetadataDto`
  - `ParserCarbonFactorImportItem`
  - `ParserProvenanceMetadataDto`
- `POST /carbon-factors/import` endpoint boundary exists and returns `202 Accepted` for accepted batches.
- Boundary service validates request-level and row-level constraints deterministically.
- Mixed-batch behavior is implemented (accepted rows can coexist with rejected rows).
- Duplicate handling is implemented for both:
  - duplicate `external_factor_id`
  - duplicate normalized factor identity
- Deterministic import result summary fields are implemented.
- Deterministic boundary-level audit metadata is included in accepted responses.
- Response contract preserves `persisted=false` and `import_execution="not_started"`.

## Explicit non-goals preserved

The current ingestion flow deliberately does **not** implement:

- persistence/database writes,
- import execution orchestration,
- background queue/job processing,
- durable audit storage,
- parser project runtime changes,
- authentication/authorization,
- tenant/company scoping,
- performance/load validation.

## Contract and versioning review

- Contract versioning is explicit via `contract_version`.
- Current supported contract version is deterministic and narrow (`"1.0"`).
- Unsupported versions are rejected with deterministic invalid-query behavior.
- Contract serialization is covered with snake_case JSON property tests.

Readiness posture:
- Positive: clear version gate prevents silent drift.
- Gap: no multi-version compatibility strategy yet (future task when a second version is introduced).

## Validation model review

- Batch-level validation enforces required boundary metadata (`batch_id`, source metadata, publication/version, contract version).
- `parser_metadata` has conditional required fields (`parser_name`, `parser_version` when parser metadata is present).
- Row-level validation enforces required factor fields, non-negative factor value, and bounded optional year range.
- Validation output is deterministic and ordered by row index, field, and code.

Readiness posture:
- Positive: deterministic validation supports repeatable integration behavior.
- Gap: boundary validation is present, but no persistence-stage validation exists because persistence is out of scope.

## Duplicate handling review

- Duplicate `external_factor_id` is rejected within batch.
- Duplicate factor identity is rejected within batch using normalized identity fields.
- Duplicate identity uses deterministic normalization for optional fields (blank/null handled consistently).

Readiness posture:
- Positive: deterministic duplicate policy is explicit and testable.
- Gap: no cross-batch or database-level deduplication exists (no persistence implemented yet).

## Mixed-batch policy review

- If at least one row is valid, endpoint returns `202 Accepted` and includes accepted/rejected counts with row-level warnings/errors.
- If all rows are invalid, endpoint returns deterministic `400 invalid_query` (`no valid factor rows remain after validation`).

Readiness posture:
- Positive: mixed-batch policy is deterministic and integration-safe.
- Gap: operational handling for partial acceptance after persistence/import execution is not implemented yet.

## Result reporting review

- Response includes deterministic aggregate fields:
  - `total_records`, `accepted_records`, `rejected_records`
  - `warning_count`, `error_count`
  - `has_warnings`, `has_errors`
  - `status`, `validation_status`
- Validation statuses are deterministic (`accepted`, `accepted_with_warnings`, `accepted_with_validation_errors`).

Readiness posture:
- Positive: summary contract is machine-readable and testable.
- Gap: no downstream import execution state transitions beyond `import_execution="not_started"`.

## Audit metadata review

- Accepted responses include boundary-level deterministic `audit` metadata.
- `audit_id` is deterministic from canonical boundary identity inputs.
- `evaluated_at_utc` currently mirrors parser `generated_at_utc` when provided.

Critical scope note:
- Audit metadata is currently boundary-level response metadata only.
- There is no durable audit storage table or write path in this scope.

## Security handoff risks for SEC-001/SEC-002

1. **Unauthenticated ingestion endpoint**: import boundary is currently callable without auth controls.
2. **No tenant scoping**: no requester-to-tenant association is enforced.
3. **No authorization model**: no role/scope policy for who can submit imports.
4. **No durable audit trail for security investigations**: response audit metadata is not persisted.
5. **No request-rate protection strategy documented**: abuse/throttling posture is not yet defined.

## Performance/test handoff risks for PT-002

1. No ingestion load/performance baseline exists for large batches.
2. No documented SLOs for validation latency/error rates.
3. No stress posture for high-warning/high-error payloads.
4. No capacity posture for deterministic hashing + validation at scale.

## Operational risks

1. **Integration expectation risk**: endpoint returns accepted boundary responses even though no data is persisted.
2. **Audit retention risk**: audit metadata can be observed in responses but is not durable for later retrieval.
3. **Supportability gap**: no import execution lifecycle, retries, or failure recovery model exists.
4. **Consumer misunderstanding risk**: `persisted=false` must remain explicit in downstream integration documentation.

## Remaining production gaps

- Authentication and authorization.
- Tenant/company scoping.
- Durable import/audit persistence model.
- Actual import execution lifecycle and idempotency semantics.
- Operational observability for ingestion throughput/failure posture.
- Performance/load testing and capacity sizing.

## Recommended next tasks

1. **SEC-001/SEC-002**: add authenticated import boundary and tenant-aware authorization/scoping.
2. **ING follow-up**: introduce persistence/import execution lifecycle without breaking boundary contract invariants.
3. **PT-002**: define ingestion performance SLOs and execute representative load testing.
4. **OPS follow-up**: add durable audit retention and ingestion observability (metrics/logs/alerts).

## Merge guidance for ING-007

This review indicates ingestion boundary behavior is consistent and deterministic for current scope and is suitable for controlled integration/security hardening. It is not a claim of end-to-end production readiness.

## SEC-002 update (tenant scoping boundary model)

- Import endpoint security now includes deterministic tenant identity resolution from config.
- Required configuration for protected import boundary:
  - `Security:ApiKey:ImportEndpointKeyHash`
  - `Security:ApiKey:ImportTenantId`
- Missing tenant configuration fails closed with deterministic `401 unauthorized`.
- Accepted boundary responses expose tenant context in audit metadata (`tenant_id`, `authentication_scheme`).
- Scope remains boundary-only:
  - no DB-backed tenant store,
  - no import persistence/execution,
  - no tenant-scoped read filtering.
