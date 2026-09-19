# Product and engineering roadmap

This roadmap keeps the real job-search workflow as the first test case while preserving a path to a generalized multi-user product. Milestones are ordered by dependency and learning value, not by novelty.

## Current baseline

V0.1 currently supports:

- a seeded candidate profile built from verified evidence
- manual job entry
- basic field normalization
- deterministic eligibility, fit, and application-priority scoring
- inspectable explanations and scoring factors
- PostgreSQL persistence through EF Core
- an Angular review interface
- a local browser-based database viewer

## Next: V0.1 hardening

Goal: make the current slice dependable enough to use for real job evaluation and safe to evolve.

Deliverables:

1. Replace `EnsureCreated` with versioned EF Core migrations.
2. Add API integration tests backed by PostgreSQL/Testcontainers.
3. Add request validation and consistent API error responses.
4. Move local configuration into documented environment-based settings.
5. Add structured logs and correlation identifiers for a submitted job.
6. Add a small, versioned scoring fixture set to detect ranking regressions.
7. Add CI that builds the API and UI and runs tests for every pull request.

Exit criteria:

- a clean database can be created entirely from migrations
- API and scoring behavior are covered by automated tests
- a pull request cannot merge when builds or tests fail

## V0.2: candidate evidence

Goal: replace hard-coded candidate context with evidence that the user can inspect and maintain.

Deliverables:

1. Define evidence source, provenance, verification status, confidence, and timestamps.
2. Add candidate-profile and evidence API endpoints.
3. Add Angular screens for reviewing and editing profile preferences and evidence.
4. Require scoring explanations to identify the evidence they used.
5. Import structured evidence from a resume without silently accepting generated claims.

Exit criteria:

- every candidate claim used by scoring points to verified evidence
- edits are persisted and reflected in subsequent job evaluations
- unsupported claims cannot enter generated output unnoticed

## V0.3: application workflow

Goal: carry strong jobs through a useful, measurable application lifecycle.

Deliverables:

1. Finalize job and application lifecycle states and allowed transitions.
2. Convert a reviewed job into an application.
3. Track dates, status, resume version, notes, and next action.
4. Record rejection, screen, interview, offer, and withdrawal outcomes.
5. Add a focused dashboard for active work and outcome metrics.

Exit criteria:

- the real job search can be managed without a parallel spreadsheet
- every active application has a visible state and next action
- outcome data can be summarized without manual reconciliation

## V0.4: semantic scoring and evaluation

Goal: use a model only where semantic judgment adds measurable value.

Deliverables:

1. Introduce a provider-neutral model interface.
2. Add structured semantic comparison between requirements and evidence.
3. Keep deterministic score, semantic score, and final recommendation separately inspectable.
4. Build a replayable labeled job set from actual review decisions.
5. Compare scoring changes against precision, recall, ranking quality, cost, and latency.
6. Require human approval for model-proposed profile or resume changes.

Exit criteria:

- semantic scoring improves the labeled evaluation set over deterministic scoring alone
- recommendations cite evidence and can be reproduced from stored inputs
- model failure degrades gracefully to deterministic behavior

## V0.5: generalized multi-user product

Goal: evolve the proven single-user workflow into a secure product boundary.

Deliverables:

1. Add OIDC authentication and stable user identity.
2. Add ownership and tenant boundaries to persisted entities.
3. Enforce authorization in APIs and test cross-user isolation.
4. Add secure secrets, deployment configuration, backups, and retention rules.
5. Introduce asynchronous workers and queues only for workflows that need them.
6. Add observability with OpenTelemetry and production dashboards.

Exit criteria:

- users can only access their own profiles, jobs, applications, and artifacts
- production operations have documented recovery and data-handling procedures
- background work is retryable, idempotent, and observable

## Deferred until justified

- automated job-board discovery
- browser-driven application submission
- Kubernetes, Kafka, or Redis
- autonomous resume changes or application submission

These features should enter the roadmap only when the preceding workflow produces evidence that they solve a real bottleneck.
