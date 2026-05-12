# Task Issue Model

Task issues describe one bounded unit of work that an agent can execute on a
feature branch. The issue body should be explicit enough that the agent can
implement, validate, and report without relying on hidden context.

## Required Body Format

```text
Task ID: API-009
Lane: dotnet
Status: blocked
Agent: dotnet-agent
Depends on: API-008
Unblocks: PT-001

Scope:
...

Non-goals:
...

Allowed files:
...

Forbidden changes:
...

Acceptance criteria:
...

Required final report:
...
```

## Field Guidance

- `Task ID`: stable identifier used in branch names, commits, PR titles, and
  final reports.
- `Lane`: primary work lane such as `python`, `dotnet`, `docs`, `ops`,
  `parity`, or `review`.
- `Status`: current task state matching the `status:*` labels.
- `Agent`: intended agent or contributor owner, when known.
- `Depends on`: task IDs that should land before this work starts or merges.
- `Unblocks`: task IDs that can proceed after this task lands.
- `Scope`: the work to complete.
- `Non-goals`: related work that must stay out of the task.
- `Allowed files`: expected write boundaries.
- `Forbidden changes`: guardrails such as no runtime behavior changes, no
  generated artifacts, or no historical run edits.
- `Acceptance criteria`: observable completion checks.
- `Required final report`: exact reporting sections expected from the agent.

## Branch And PR Model

Agents should create task branches from `develop` and open pull requests back
to `develop`. The user remains responsible for review and merge. Automation for
watching ready tasks or dispatching agents is intentionally out of scope for
this foundation.
