# Task Labels

This taxonomy prepares CarbonOps-API for task-driven agent work. Labels are
documented here for consistency; this task does not create labels through the
GitHub API.

## Status Labels

- `status:blocked`: Work cannot start until a dependency, decision, or external
  input is resolved.
- `status:ready`: Work is sufficiently specified and can be picked up by an
  agent.
- `status:in-progress`: An agent or contributor is actively working the task.
- `status:in-review`: A pull request is open and ready for user review.
- `status:needs-fix`: Review, validation, or CI found changes required before
  merge.
- `status:merged`: The task PR has been merged into the target branch.

## Lane Labels

- `lane:python`: Python implementation work under `src/python`.
- `lane:dotnet`: .NET implementation work under `src/dotnet`.
- `lane:docs`: Documentation-only work.
- `lane:ops`: Repository workflow, issue model, labels, CI, or automation
  foundation work.
- `lane:parity`: Cross-implementation contract, behavior, or evidence work.
- `lane:review`: Review, checkpoint, audit, or cleanup work.

## Task ID Prefixes

- `API`: product/API tasks that affect the CarbonOps-API contract, behavior,
  docs, or platform direction.
- `API-OPS`: workflow, repository operations, CI, automation, and task model
  tasks.
- `PY`: Python implementation lane tasks if a shorter lane-specific sequence is
  needed later.
- `DN`: .NET implementation lane tasks if a shorter lane-specific sequence is
  needed later.
- `PT`: parity tasks that compare, align, or evidence Python and .NET behavior.
- `RV`: review and checkpoint tasks.

## Label Usage

Every task issue should have one status label and at least one lane label. A
task may have multiple lane labels when it explicitly coordinates across
implementation paths, docs, and review.
