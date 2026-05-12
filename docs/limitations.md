# Limitations

CarbonOps-API is currently a documentation-baseline and pre-alpha contract
foundation. It has clear limits.

## Non-Goals

CarbonOps-API does not:

- calculate carbon inventories
- produce emissions reports
- certify source data correctness
- replace source-owner documentation or files
- provide production deployment infrastructure
- execute CarbonOps-Parser jobs
- provide CarbonOps-Web frontend behavior
- guarantee regulatory, audit, or compliance readiness

## Current Technical Limits

- Synthetic in-memory data only.
- Thin local FastAPI adapter only.
- No database persistence.
- No authentication or authorization.
- No audit logging.
- No rate limiting.
- No parser execution.
- No production runtime posture.

## Documentation Limits

The documentation describes the intended direction for .NET-first clean
architecture. The repository now includes a current Python implementation root
and a planned .NET root, but it does not claim a finished .NET implementation.
