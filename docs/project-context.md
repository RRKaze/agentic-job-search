# Project Context and Progress

Last canonicalized: 2026-09-12

This document records the durable product context, current progress, and major decisions for the Agentic Job Search Workflow. `AGENTS.md` contains concise instructions for coding agents; this file provides the fuller context behind those instructions.

## 1. Two-project model

The work is intentionally separated into two related projects.

### Job Search

The real-world career workflow:

- resume development
- job discovery and evaluation
- applications
- interview preparation
- application outcomes and rejections
- feedback-driven iteration

### Agentic Job Search Workflow

This repository is the engineering system supporting that workflow.

It should consume or synchronize verified information from Job Search so the user does not repeatedly re-enter candidate evidence, preferences, jobs, applications, and outcomes.

The engineering project must not become the authority for whether a candidate claim is true. Verified Job Search evidence remains authoritative.

## 2. Product success criteria

The system has two explicit success criteria.

### Real-world usefulness

It should materially improve job-search efficiency and outcomes. Useful measurements may eventually include:

- time spent discovering viable roles
- percentage of surfaced jobs worth reviewing
- application throughput
- screen/interview conversion
- reasons jobs are rejected before application
- feedback-driven changes in ranking quality

### Engineering quality

The project should reach sufficient depth and quality to be publicly showcased on GitHub and potentially included as a project on a Senior Software Engineer resume.

Resume value must come from real architecture, state management, scoring, data modeling, evaluation, and feedback loops—not artificial complexity.

## 3. Candidate positioning

Current canonical positioning:

- Primary level: Senior Software Engineer
- Strongest families: Backend, Platform, Identity/Authentication, Distributed Systems
- Selective backup: SWE III / mid-senior roles with strong fit
- Staff/Principal: not a primary target
- Sponsorship: not required; candidate is a U.S. citizen
- Support/on-call: unusually heavy support/on-call expectations should reduce ranking/application priority

The system should emphasize backend/platform/identity/distributed-systems depth rather than generic full-stack breadth.

## 4. Verified candidate evidence

The system may use the following evidence as currently verified context, subject to future corrections.

### Current role and stack

- Senior Software Engineer at UKG, Jan 2022–present
- C#, .NET / ASP.NET Core
- REST APIs
- SQL
- React / TypeScript / JavaScript production experience
- Python experience/project use

### Authentication modernization

- Authentication modernization/migration supporting 1,000+ enterprise companies/tenants
- Staged readiness checks
- Remediation before/during migration
- Asynchronous synchronization
- Validation
- Feature-flag recovery

### Messaging and distributed processing

- Authentication service functions as both producer and consumer
- Uses the company’s internally built/self-hosted messaging/eventing system
- Jobs are enqueued and picked up by backend C# workers/processors
- Significant data processing and data cleaning occurs in these flows
- Workflows mix asynchronous and synchronous processing
- Selective retry is used for transient failures
- Data-validation failures can require remediation rather than blind retries

### Transaction/recovery behavior

- Processing includes user-level transactional semantics
- Incomplete operations can be rolled back on timeout while preserving migration recovery behavior
- Do not infer a specific implementation mechanism such as `.NET TransactionScope` unless confirmed

### Production engineering

- Deep production/system/data diagnosis across authentication migration workflows
- Targeted remediation and systemic follow-up
- SRE/production incident experience
- Customer escalation experience

### Product and collaboration

- Product/design partnership works backward from desired outcomes to identify technical gaps, dependencies, edge cases, and timeline tradeoffs
- Mentoring, onboarding, and documentation are supporting evidence
- Cross-team collaboration at 20+ team scale appeared in prior resume material; treat cautiously and verify before emphasizing

### CI/CD

- TeamCity CI/CD automation experience
- A prior claim of approximately 40% deployment/operational-effort improvement exists; retain only when evidence remains supportable

## 5. Evidence integrity requirements

Candidate evidence is a hard safety boundary for this product.

The system must not invent or inflate:

- technologies
- years of experience
- ownership
- architectural responsibility
- metrics
- scale
- business outcomes
- implementation mechanisms

Job descriptions may influence which verified evidence is selected and how it is framed, but must never cause unsupported evidence to be generated.

A future implementation should make provenance/verification explicit in the candidate data model rather than relying only on prompt instructions.

## 6. Job Search state relevant to the system

The real Job Search currently has:

- 0 applications
- 0 screens
- 0 interviews
- 0 offers

This is an initial state rather than negative market feedback; the candidate/resume foundation was established before starting targeted applications.

Current real-world sequence:

1. Finish usable Senior SWE resume v2.
2. Submit the first 2–3 strong-fit targeted applications.
3. Record outcomes and feedback.
4. Iterate ranking/resume strategy from real market signals.
5. Move toward a sustainable target of roughly 1–2 applications per day.

The existing career tracker conceptually contains:

- Jobs
- Applications
- Feedback
- Resume Versions
- Project Roadmap
- Candidate Profile
- Dashboard

This structure is intentionally useful input for future backend/domain modeling, but the engineering implementation should refine the schemas rather than mechanically copy spreadsheet columns.

## 7. Architecture decisions established so far

### Workflow over giant prompt

Use explicit workflow/state transitions rather than a single agent prompt attempting discovery, scoring, tailoring, tracking, and application at once.

### Core domain entities

Initial contracts should cover at minimum:

- `CandidateProfile`
- `Job`
- `Application`
- `Feedback`

Additional entities/value objects should be introduced only as the domain requires them.

### Hybrid scoring

Scoring should combine:

1. hard filters
2. deterministic scoring
3. LLM semantic scoring

Deterministic rules should handle things that do not require model judgment. LLM evaluation should be reserved for semantic comparisons and reasoning where it adds value.

### Fit versus priority

`fit_score` and `application_priority` are different concepts and must remain separate.

A job may be a strong skills match but low priority because of factors such as support burden, application effort, undesirable working conditions, or other preferences.

### Persistence

Start with SQLite for v0.1.

PostgreSQL is a possible later migration if concurrency, deployment, multi-user behavior, scale, or other requirements justify it.

### Automation boundary

Browser/application automation is intentionally not an early milestone.

First establish reliable discovery/scoring/tracking/feedback. Any future submission automation should preserve human approval before an application is actually submitted.

## 8. Current engineering roadmap

| Workstream | State | Approx. progress |
| --- | --- | ---: |
| Goals & success criteria | Complete | 100% |
| System architecture | In progress | 25% |
| Data contracts / schemas | Not started | 0% |
| Scoring engine | Not started | 0% |
| Persistence | Not started | 0% |
| Resume tailoring | Not started | 0% |
| Browser automation | Deferred | 0% |

## 9. Immediate v0.1 milestone

Before building substantial application code:

1. define system boundaries
2. define the primary data flow
3. define `CandidateProfile`
4. define `Job`
5. define `Application`
6. define `Feedback`
7. identify state transitions and invariants
8. identify what scoring is deterministic versus model-assisted

After these contracts are understood, implementation should proceed in small testable slices.

## 10. Near-term design questions

These are intentionally unresolved and should be decided through architecture work rather than assumed:

- What is the canonical source/import format for candidate evidence?
- How should evidence provenance and verification status be represented?
- What lifecycle states should a `Job` have before it becomes an `Application`?
- Should rejected/ignored jobs remain stored to improve future ranking?
- What feedback taxonomy provides useful learning without becoming burdensome?
- Which hard filters are absolute versus configurable penalties?
- How should deterministic and semantic scores be normalized and explained?
- What evaluation dataset or replay mechanism can measure ranking improvements over time?
- What is the first useful interface: CLI, local web UI, API, or a combination?

## 11. Guiding constraint

Every major feature should answer at least one of these questions:

- Does this improve the real job search?
- Does this make the system more correct, measurable, maintainable, or explainable?

If it does neither, it probably does not belong in v0.1.
