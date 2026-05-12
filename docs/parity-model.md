# Parity Model

The future .NET and Python implementation paths should behave consistently at
the public API and domain contract level while remaining independent codebases.

## Parity Goals

- Same documented routes or route equivalents.
- Same CarbonFactor field semantics.
- Same supported query filters.
- Same deterministic error codes for equivalent failures.
- Same response status expectations.
- Same public limitation language.

## Allowed Differences

The implementations may differ in:

- project layout
- framework choice
- dependency injection style
- test framework
- serialization internals
- deployment packaging

These differences are acceptable when public behavior remains aligned.

## Evidence Expectations

Future parity evidence should include:

- contract tests shared by behavior, not source imports
- route and OpenAPI comparison where applicable
- sample request/response fixtures
- error-case comparison
- documentation updates when behavior changes

## Current Status

API-001 defines the parity model only. It does not add a .NET implementation,
create `src/python`, or add cross-language test infrastructure.
