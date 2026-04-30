# Evidence Backlog (Pre-Alpha)

| Task | GTV evidence relevance | Risk level | Implementation note |
|---|---|---|---|
| Add contract validation matrix tests | Improves traceability of deterministic behavior claims | Medium | Expand edge-case tests for query combinations and invalid inputs |
| Add transport adapter spike | Demonstrates contract-to-HTTP mapping readiness evidence | Medium | Add minimal adapter layer without changing service contracts |
| Add fixture versioning strategy | Supports repeatable verification over dataset changes | Low | Define version labels for synthetic and future parsed datasets |
| Add integration seam for parser-fed data | Shows future data lineage path for lookup behavior | Medium | CarbonFactor Parser can later supply parsed factor records via local importable data package or artifact |
| Add consumer scenario docs | Supports usage evidence for assistant-driven workflows | Low | CarbonOps Assistant can later consume API-like lookup behavior through stable contract interfaces |


Transport-boundary evidence has been added in local deterministic tests for envelope serialization, status mapping, and handler behavior.

- Evidence: HTTP adapter parity tests cover list, detail, invalid filter, and not-found behavior against transport envelopes.

- Evidence: OpenAPI inspection tests verify deterministic metadata/path presence without publishing generated artifacts.
- Evidence: HTTP contract tests verify supported query keys and deterministic invalid-query envelope behavior.
