# Release Candidate Checkpoint (REL-002)

Date: 2026-05-22

## Purpose And Scope

REL-002 is the production release candidate checkpoint for the current CarbonOps-API baseline.

This checkpoint records the final review position, validation checklist, controlled/internal pilot constraints, broad production blockers, and operator handoff references for the current `develop` baseline.

REL-002 is not a production deployment, broad production approval, GitHub release, version tag, release automation task, runtime implementation task, or generated artifact task.

## Checkpoint Summary

| Checkpoint Area | Position |
| --- | --- |
| Controlled/internal pilot checkpoint | GO with constraints. |
| Broad production | NO-GO. |
| Production deployment performed | NO. |
| Release automation performed | NO. |
| Runtime changes in REL-002 | NO. |
| Generated artifacts committed | NO. |

## Baseline Under Review

Repository:

```text
ktalpay/CarbonOps-API
```

Target branch:

```text
develop
```

Current evidence chain:

- SEC-007: security production readiness review.
- OPS-033: production runbook.
- PT-004: production parity review.
- DOC-004: production readiness documentation.
- REL-001: production readiness review.

Primary review reference:

- `docs/production-readiness-review.md`

## Required Validation Checklist

REL-002 validation should be run from the repository root unless a command says otherwise.

| Validation | Command | Expected Result Category | REL-002 Result |
| --- | --- | --- | --- |
| Whitespace/diff hygiene | `git diff --check` | Pass | Pass |
| Local git state before commit | `git status --short` | Only REL-002 files plus known local untracked files | Pass |
| API tests | `dotnet test src/dotnet/tests/CarbonOps.Api.Tests/CarbonOps.Api.Tests.csproj` | Pass | Pass, with known existing xUnit1013 warning |
| Contracts tests | `dotnet test src/dotnet/tests/CarbonOps.Contracts.Tests/CarbonOps.Contracts.Tests.csproj` | Pass | Pass |
| Full .NET solution tests | `dotnet test src/dotnet/CarbonOps.Api.sln` | Pass | Pass |
| Python docs/fixture tests | `python -m pytest -q ...` from `src/python` | Pass | Pass |
| Parity manifest JSON | `python -m json.tool tests/contract-parity/contract-parity-baseline.json >/dev/null` | Pass | Pass |
| OpenAPI metadata JSON | `python -m json.tool tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json >/dev/null` | Pass | Pass |
| HTTP fixture JSON | `for file in tests/contract-fixtures/http/*.json; do python -m json.tool "$file" >/dev/null; done` | Pass | Pass |
| Package script syntax | `bash -n scripts/ops/validate-dotnet-package.sh` | Pass | Pass |
| Package prerequisite check | `scripts/ops/validate-dotnet-package.sh --check-only` | Pass if Docker is available | Pass |
| Docker full image build | `IMAGE_TAG=carbonops-api:rel-002 scripts/ops/validate-dotnet-package.sh` | Pass if Docker is available and time allows | Pass |
| Container health smoke | `docker run ...` then `curl -i http://localhost:8080/health` | Pass if Docker full build is run | Pass |

Docker full image build and container health smoke are part of this checkpoint only when Docker is available locally. Do not report them as complete unless the commands were actually run.

## Checkpoint Acceptance Criteria

REL-002 acceptance criteria:

- no generated artifacts are staged;
- no runtime behavior changes are included;
- changes are documentation and docs-test only unless a small link fix is needed;
- validation evidence is recorded;
- controlled/internal pilot constraints remain explicit;
- broad production blockers remain visible;
- production deployment is not performed by this PR;
- release tags, GitHub releases, and deployment automation are not created by this PR.

## Controlled/Internal Pilot Constraints

The controlled/internal pilot checkpoint is GO with constraints only when:

- operators are known and accountable;
- import clients are known, few, and controlled;
- API keys are high entropy and distributed through approved secret channels;
- runtime configuration is external to the Docker image;
- `ASPNETCORE_ENVIRONMENT=Production` enables Production startup validation;
- public read endpoint data is accepted as safe for unauthenticated access;
- in-process rate limiting is accepted for the pilot deployment topology;
- `docs/production-runbook.md` is followed;
- rollback image and configuration paths are prepared before traffic is enabled;
- structured logs, audit event logs, rate-limit signals, and `correlation_id` values are monitored;
- no Python modern parity claim is made.

## Broad Production Blockers

Broad production remains NO-GO because these blockers remain open:

- durable audit persistence;
- distributed or edge-aware rate limiting;
- DB-backed token lifecycle;
- secret manager integration;
- external monitoring and SIEM/export integration;
- import persistence and execution lifecycle;
- read authorization and tenant filtering if read data becomes sensitive;
- generated OpenAPI and generated client strategy if external consumers need it;
- Python modern API parity if Python is production-facing;
- deployment automation and infrastructure provisioning.

## Go/No-Go Decision

Controlled/internal pilot checkpoint: GO with constraints.

Broad production release: NO-GO.

REL-002 does not approve broad production and does not mark CarbonOps-API as production deployed.

Release owners must make the final deployment decision outside this PR. That decision must explicitly accept the controlled/internal pilot constraints and the remaining broad production blockers.

## Operator Handoff

Operator and release owner references:

- `docs/production-runbook.md`
- `docs/deployment-packaging.md`
- `docs/environment-config-hardening.md`
- `docs/production-readiness.md`
- `docs/production-readiness-review.md`

## Non-Goals

REL-002 does not add:

- production deployment;
- GitHub releases or tags;
- runtime changes;
- generated artifacts;
- deployment automation;
- broad production approval;
- public response body changes;
- fixture changes;
- OpenAPI metadata baseline changes;
- Dockerfile or script changes;
- generated OpenAPI output;
- generated clients;
- database migrations;
- secret manager integration.
