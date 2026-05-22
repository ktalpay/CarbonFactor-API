# Production Readiness Review (REL-001)

Date: 2026-05-22

## Purpose And Scope

REL-001 is the full production readiness review for the current CarbonOps-API baseline.

This review uses evidence from the SEC, OPS, PT, DOC, and DB work lines. It consolidates the current security, persistence, observability, packaging, contract, parity, endpoint, runbook, and readiness documentation into a release-readiness recommendation.

This document does not approve broad production by itself. It does not implement runtime changes, public contract changes, fixture updates, deployment automation, generated artifacts, migrations, secret manager integration, or release execution. REL-002 remains the release decision and checkpoint follow-up.

## Evidence Reviewed

| Evidence | Review Use |
| --- | --- |
| `docs/production-readiness.md` | DOC-004 readiness checklist, production assumptions, go/no-go guidance, and REL-001 handoff. |
| `docs/security-production-readiness.md` | SEC-007 review of API key auth, tenant/scope config, hash verification, revoke/rotation, leakage controls, and security gaps. |
| `docs/persistence-production-readiness.md` | DB-008 persistence review and controlled-integration verdict for in-memory/PostgreSQL boundaries. |
| `docs/observability-readiness.md` | OPS-026 through OPS-030 logging, correlation id, audit event, rate limiting, and versioned-route observability baseline. |
| `docs/environment-config-hardening.md` | OPS-031 Production startup validation and secret-boundary rules. |
| `docs/deployment-packaging.md` | OPS-032 Docker packaging model, runtime config boundary, and package validation. |
| `docs/production-runbook.md` | OPS-033 operator procedures for config, health checks, smoke checks, key rotation, revoke, troubleshooting, rollback, and security handling. |
| `docs/production-parity-review.md` | PT-004 verdict that .NET contract coverage is controlled while Python modern API parity is incomplete. |
| `docs/public-endpoints.md` | DOC-001 endpoint guide for `/v1`, legacy, import, operational, header, rate-limit, and error behavior. |
| `docs/architecture.md` | DOC-002 architecture overview across API, persistence, security, ingestion, observability, packaging, and parity. |
| `docs/developer-setup.md` | DOC-003 local validation, fixture, package, generated artifact, and secret hygiene workflow. |
| `docs/response-fixtures.md` | PT-002 deterministic response fixture scope and update policy. |
| `docs/openapi-contract-drift.md` | PT-003 public route metadata drift check scope and limitations. |
| `tests/contract-parity/contract-parity-baseline.json` | PT-001 route family, field group, error category, header, and non-goal baseline. |
| `tests/contract-fixtures/http/` | PT-002 checked-in HTTP response fixtures for representative contract shapes. |
| `tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json` | PT-003 public route/method/auth/rate-limit metadata baseline. |
| `Dockerfile` | OPS-032 .NET 8 multi-stage container packaging baseline. |
| `scripts/ops/validate-dotnet-package.sh` | Local packaging validation helper for Docker prerequisites and image build. |

## Review Method

This review:

- checked the current readiness, security, persistence, observability, packaging, runbook, endpoint, architecture, parity, fixture, and metadata docs;
- checked the current route/fixture/metadata evidence paths;
- reviewed the DOC-004 readiness checklist and go/no-go guidance;
- reviewed expected validation commands for .NET, Python docs/fixtures, JSON fixtures, and optional Docker packaging;
- preserves the current runtime and contract behavior without code changes.

REL-001 is evidence-based but still a documentation handoff. REL-002 should run or review current validation output before making a release decision.

## Current Readiness Verdict

| Area | Verdict | Recommendation |
| --- | --- | --- |
| Controlled/internal pilot | GO with constraints | Proceed only under the controlled-pilot constraints below. |
| Broad production exposure | NO-GO | Do not broadly expose the API until blockers are addressed or formally accepted. |
| Python/.NET production equivalence | NO-GO | Do not describe Python as production-equivalent to the modern .NET API. |
| Import persistence/execution readiness | NO-GO | Do not treat accepted imports as persisted or executed. |
| REL-002 release checkpoint readiness | GO for review/checkpoint | Proceed to REL-002 as a controlled/internal pilot checkpoint, not broad production approval. |

## Controlled/Internal Pilot Constraints

A controlled/internal pilot is acceptable only when:

- operators are known and accountable;
- import clients are known, few, and controlled;
- API keys are high entropy and distributed through approved secret channels;
- runtime configuration is external to the Docker image;
- `ASPNETCORE_ENVIRONMENT=Production` is used so Production startup validation is enabled;
- public read endpoint data is accepted as safe for unauthenticated access;
- in-process rate limiting is accepted for the pilot deployment topology;
- `docs/production-runbook.md` is followed for deployment, smoke checks, rotation, revoke, incidents, and rollback;
- rollback image and configuration paths are prepared before pilot traffic;
- structured logs, audit event logs, rate-limit signals, and `correlation_id` values are monitored;
- no one claims Python modern API parity or Python/.NET production equivalence.

## Broad Production Blockers

Broad production exposure remains blocked by:

- durable audit persistence;
- distributed or edge-aware rate limiting;
- DB-backed token registry and lifecycle;
- secret manager integration;
- external monitoring, alerting, and SIEM/export integration;
- import persistence and execution lifecycle;
- read authorization and tenant filtering if read data becomes sensitive;
- generated OpenAPI and generated client strategy if external consumers need it;
- Python modern API parity if Python is production-facing;
- deployment automation and infrastructure provisioning.

## Security Review Result

The current security baseline is acceptable for a controlled/internal pilot only.

Strengths:

- import endpoints require `X-Api-Key`;
- active API key config is hash-only through lowercase SHA-256 hex;
- current, previous, and revoked hashes support config-driven rotation and emergency revoke;
- revoked hashes are checked before current/previous acceptance;
- tenant id and required import scope are config-driven;
- Production startup validation rejects missing, unsafe, placeholder, or plaintext security-critical config;
- public responses and logs are designed not to leak API keys, hashes, configured scopes, or raw secret-bearing inputs.

Remaining security gaps:

- no DB-backed token lifecycle;
- no per-token owner, tenant/company, scope, created time, expiry, or last-used metadata;
- no token generation API;
- no token expiry enforcement;
- no durable auth decision audit persistence.

## Persistence And Import Review Result

The read path has a defined persistence boundary:

- default mode is in-memory with deterministic reference data;
- PostgreSQL mode is opt-in through `Persistence:UsePostgreSql=true`;
- PostgreSQL mode requires an explicit connection string;
- DB-008 reviews persistence as ready for controlled integration work, not full production readiness.

The import path is not production-persistence ready:

- import remains boundary acceptance and validation only;
- accepted import responses keep `persisted=false`;
- accepted import responses keep `import_execution="not_started"`;
- there is no production import execution lifecycle, write path, data publication workflow, or durable import audit persistence.

## Observability And Operations Review Result

The current operations baseline supports a controlled/internal pilot:

- structured logs use named fields through the built-in ASP.NET Core logging stack;
- `X-Correlation-Id` is returned on success, error, auth failure, validation failure, and rate-limit responses;
- logging-backed audit events cover import authorization failure, import validation failure, accepted import, and rate-limit rejection;
- rate-limit rejection logs and audit events avoid raw headers, keys, hashes, query strings, and request bodies;
- Docker packaging and the production runbook exist;
- Production startup validation fails fast for unsafe critical config.

Remaining operations gaps:

- no durable audit persistence;
- no external SIEM/export integration;
- no distributed tracing or OpenTelemetry;
- no alerting or production monitoring baseline;
- no distributed/edge rate limiting;
- no infrastructure provisioning or deployment automation.

## API, Contract, And Parity Review Result

The .NET public HTTP contract is controlled for the current baseline:

- `/v1` and legacy carbon factor routes are documented and tested;
- operational endpoints remain unversioned;
- public response shapes are covered by PT-002 fixtures;
- public route/method/auth/rate-limit metadata is covered by PT-003;
- `X-Correlation-Id`, `Retry-After`, and import `X-Api-Key` behavior are documented;
- error envelopes are documented and fixture-covered for representative cases.

Remaining API and parity gaps:

- no full generated OpenAPI schema artifact;
- no generated clients;
- Python modern API parity is incomplete and documented;
- Python remains a legacy/local read-only `/factors` foundation unless future work changes that scope.

## Validation Evidence

Recommended REL-001 and REL-002 validation commands:

```bash
git diff --check
dotnet test src/dotnet/tests/CarbonOps.Api.Tests/CarbonOps.Api.Tests.csproj
dotnet test src/dotnet/tests/CarbonOps.Contracts.Tests/CarbonOps.Contracts.Tests.csproj
dotnet test src/dotnet/CarbonOps.Api.sln
```

Python docs and fixture tests:

```bash
cd src/python
python -m pytest -q \
  tests/test_contract_parity_baseline.py \
  tests/test_response_fixtures.py \
  tests/test_production_parity_review.py \
  tests/test_public_endpoints_doc.py \
  tests/test_architecture_doc.py \
  tests/test_developer_setup_doc.py \
  tests/test_production_readiness_doc.py \
  tests/test_production_readiness_review_doc.py
```

JSON fixture validation from the repository root:

```bash
python -m json.tool tests/contract-parity/contract-parity-baseline.json >/dev/null
python -m json.tool tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json >/dev/null
for file in tests/contract-fixtures/http/*.json; do
  python -m json.tool "$file" >/dev/null
done
```

Docker package validation if Docker is available:

```bash
bash -n scripts/ops/validate-dotnet-package.sh
scripts/ops/validate-dotnet-package.sh --check-only
IMAGE_TAG=carbonops-api:rel-001 scripts/ops/validate-dotnet-package.sh
```

Container health smoke check if Docker build is run:

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  carbonops-api:rel-001

curl -i http://localhost:8080/health
```

Do not report Docker validation as complete unless Docker was available and the command was actually run.

## Release Recommendation

Recommend proceeding to REL-002 as a controlled/internal pilot checkpoint.

Do not recommend broad production release at this baseline.

Release owners must explicitly accept the documented constraints and gaps before any controlled/internal pilot traffic is enabled. If the intended release scope expands beyond a controlled/internal pilot, REL-002 should be a no-go until the broad production blockers are closed or formally risk-accepted by the accountable owners.

## Required REL-002 Checks

REL-002 should:

- verify the latest `develop` commit under review;
- run the targeted .NET API tests;
- run the targeted .NET contracts tests;
- run the full .NET solution tests;
- run the Python docs/fixture tests;
- run JSON fixture validation;
- run Docker package validation or record why Docker was unavailable;
- verify no generated artifacts, local caches, or local environment files are staged;
- verify the Production config checklist from `docs/production-readiness.md`;
- verify runbook access and rollback image/configuration paths;
- record the final controlled-pilot go/no-go decision;
- avoid marking broad production ready unless the blockers in this review are addressed or explicitly accepted.

## Non-Goals

REL-001 does not add:

- runtime changes;
- production approval;
- release execution;
- generated artifacts;
- deployment automation;
- broad production go decision;
- Dockerfile or packaging script changes;
- generated OpenAPI output;
- generated clients;
- DB migrations;
- secret manager integration;
- fixture or metadata baseline changes;
- Python modern API parity implementation.
