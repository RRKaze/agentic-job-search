# Product and engineering roadmap

This roadmap keeps the real job-search workflow as the first test case while preserving a path to a generalized multi-user product. Milestones are ordered by dependency and learning value, not by novelty.

## Current baseline

V0.1 currently supports:

- account registration, sign-in, and first-login onboarding
- an editable candidate profile built from verified evidence
- manual job entry
- basic field normalization
- profile-driven deterministic eligibility, fit, and application-priority scoring
- versioned evaluations with inspectable explanations and evidence snapshots
- PostgreSQL persistence through EF Core
- database-owned application workflow and outcome tracking
- an Angular dashboard, opportunity tracker, application tracker, and profile editor
- a local browser-based database viewer

## Next: V0.1 hardening

Goal: make the current slice dependable enough to use for real job evaluation and safe to evolve.

Deliverables:

- [x] Replace `EnsureCreated` with versioned EF Core migrations.
- [x] Add API integration tests backed by PostgreSQL/Testcontainers.
- [x] Add request validation and consistent API error responses.
- [x] Upgrade Angular and gate high/critical production dependency advisories in CI.
- [x] Preserve versioned job-evaluation history, scoring inputs, and verified evidence attribution.
- [x] Move local and production configuration into documented environment-based settings.
- [x] Add structured request logs and correlation identifiers.
- [x] Add a small, versioned scoring fixture set to detect ranking regressions.
- [x] Add CI that builds the API and UI and runs tests for every pull request.

Exit criteria:

- a clean database can be created entirely from migrations
- API and scoring behavior are covered by automated tests
- a pull request cannot merge when builds or tests fail

## Immediate next: free hosted daily-use beta

Goal: publish a private, installable version of the application that can be used from desktop and mobile every day without a hosting subscription.

The initial deployment uses one Render free web service for the ASP.NET Core API and compiled Angular frontend, plus Neon free PostgreSQL for durable application data. It accepts free-tier cold starts and quotas during the personal pilot. It does not accept expiring database storage, open registration, exposed administration services, or untested recovery.

Deliverables:

1. [x] Add a production container that serves the API and Angular application from one HTTPS origin.
2. [x] Add validated environment configuration, proxy handling, health checks, structured logs, and correlation identifiers.
3. [x] Add a controlled migration command for hosted releases.
4. [x] Add private registration/bootstrap controls and persistent protected session keys.
5. [x] Make the Angular application installable with a manifest, service worker, icons, update handling, and standalone display.
6. [ ] Complete a mobile usability pass and test current iOS Safari and Android Chrome.
7. [ ] Add a repeatable Render configuration and Neon setup runbook without committing credentials.
8. [ ] Add logical backup and restore tooling and complete a restore exercise.
9. [ ] Use the hosted application daily for two weeks and turn repeated friction into prioritized issues.

Exit criteria:

- the owner can install the application on a phone and complete the core job workflow
- only the owner can register for or access the hosted workspace
- deployments and application restarts preserve database state and authenticated sessions
- authenticated API responses are never stored in the service-worker cache
- a production backup can be restored into a clean database
- daily use produces concrete feedback for the next product milestone

The architecture, execution slices, security constraints, pull-request sequence, and pilot checklist are defined in [`hosted-beta-plan.md`](hosted-beta-plan.md).

## V0.2: candidate evidence

Goal: replace hard-coded candidate context with evidence that the user can inspect and maintain.

Deliverables:

1. [x] Define evidence source, provenance, and verification status.
2. [x] Add candidate-profile and evidence API endpoints.
3. [x] Add Angular screens for reviewing and editing profile preferences and evidence.
4. [x] Require scoring explanations to identify the verified evidence they used.
5. [ ] Import structured evidence from a resume without silently accepting generated claims.

Exit criteria:

- every candidate claim used by scoring points to verified evidence
- edits are persisted and reflected in subsequent job evaluations
- unsupported claims cannot enter generated output unnoticed

## V0.3: application workflow

Goal: carry strong jobs through a useful, measurable application lifecycle.

Begin editable status tracking after the database becomes the source of truth, so a later GitHub import cannot overwrite changes made in the application.

Deliverables:

1. [x] Make the database authoritative for job and application status and retire temporary GitHub ownership of those fields.
2. [x] Finalize job and application lifecycle states and allowed transitions.
3. [x] Convert a reviewed job into an application.
4. [x] Add an inline status selector to each opportunity and persist changes through the API.
5. [x] Immediately move an updated opportunity into or out of the active filtered list without requiring a page refresh.
6. [x] Track dates, status, resume version, notes, and next action.
7. [x] Record rejection, screen, interview, offer, and withdrawal outcomes.
8. [x] Add a focused dashboard for active work and outcome metrics.

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

1. [x] Add password authentication, secure cookie sessions, and stable internal user identity.
2. [x] Add ownership boundaries to persisted profile, job, evaluation, and application data.
3. [x] Enforce authorization in APIs and test cross-user isolation.
4. [ ] Add the public account lifecycle: email verification, password recovery, server-side session revocation, and optional OIDC or MFA.
5. [ ] Add account export, deletion, retention rules, and administrative support boundaries.
6. [ ] Add secure secrets, deployment configuration, backups, and recovery procedures appropriate to the hosting tier.
7. [ ] Introduce asynchronous workers and queues only for workflows that need them.
8. [ ] Add observability with OpenTelemetry and production dashboards.

Exit criteria:

- public users can verify, recover, export, and delete their accounts without administrator database access
- users can only access their own profiles, jobs, applications, and artifacts
- production operations have documented recovery and data-handling procedures
- background work is retryable, idempotent, and observable

## Deferred until justified

- automated job-board discovery
- browser-driven application submission
- Kubernetes, Kafka, or Redis
- autonomous resume changes or application submission

These features should enter the roadmap only when the preceding workflow produces evidence that they solve a real bottleneck.
