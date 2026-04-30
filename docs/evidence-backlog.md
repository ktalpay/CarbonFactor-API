# Evidence Backlog (Pre-Alpha)

## Implemented in this run

- Technical evidence index (`docs/evidence/index.md`).
- API architecture evidence summary (`docs/evidence/api-architecture-evidence.md`).
- Repository navigation alignment across core docs.

## Remaining backlog

| Task | Evidence relevance | Risk level | Conservative implementation note |
|---|---|---|---|
| Add contract validation matrix tests | Improves traceability of deterministic behavior claims | Medium | Expand edge-case tests for supported/unsupported query combinations |
| Add envelope edge-case serialization tests | Strengthens transport consistency evidence | Medium | Increase parameterized coverage for empty/optional fields and errors |
| Add fixture versioning strategy | Supports repeatable verification over dataset changes | Low | Define version labels for synthetic and future local parsed datasets |
| Define parser-data seam documentation | Clarifies future data lineage path | Medium | Describe interface only; do not introduce cross-repo runtime imports |
| Add reviewer claim-language checklist | Reduces maturity-overstatement risk | Low | Keep pre-alpha/local-only wording explicit in future docs updates |
