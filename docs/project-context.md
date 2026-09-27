# Project Context and Progress

Last canonicalized: 2026-09-27

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

The public repository ships with a fictional demo profile configured for:

- Primary level: Senior Software Engineer
- Strongest families: Backend, Platform, Identity/Authentication, Distributed Systems
- Selective backup: SWE III / mid-senior roles with strong fit
- Staff/Principal: not a primary target
- Sponsorship preference: configurable demo value
- Support/on-call: unusually heavy support/on-call expectations should reduce ranking/application priority

These are product defaults, not claims about a real person. Real identity, employment, authorization, and preference data must remain outside the public fixture.

## 4. Fictional demo evidence

The checked-in seed data exists only to demonstrate scoring behavior and must not be presented as a real person's resume.

### Current role and stack

- Senior Software Engineer at Example Corp
- C#, .NET / ASP.NET Core
- REST APIs
- SQL
- React / TypeScript / JavaScript production experience
- Python experience/project use

### Authentication modernization

- Authentication modernization/migration supporting fictional enterprise tenants
- Staged readiness checks
- Remediation before/during migration
- Asynchronous synchronization
- Validation
- Feature-flag recovery

### Messaging and distributed processing

- Authentication service functions as both producer and consumer
- Uses a fictional internal messaging/eventing system
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
- Cross-team collaboration is included as a fictional demonstration category

### CI/CD

- CI/CD automation experience
- No performance metric may be added without evidence supplied by the user

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

A new fictional demo profile starts with:

- 0 applications
- 0 screens
- 0 interviews
- 0 offers

This empty state demonstrates onboarding and must not be interpreted as a real person's job-search activity.

Representative product workflow:

1. Import or create a verified candidate profile.
2. Review and submit strong-fit applications.
3. Record outcomes and feedback.
4. Iterate ranking/resume strategy from real market signals.
5. Let each user configure a sustainable activity target.

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

Use PostgreSQL with EF Core for v0.1.

This supersedes the earlier SQLite starting point because the current direction prioritizes enterprise-transferable tooling and a cleaner path toward a generalized multi-user product. Keep the rest of the infrastructure lightweight until requirements justify it.

### Automation boundary

Browser/application automation is intentionally not an early milestone.

First establish reliable discovery/scoring/tracking/feedback. Any future submission automation should preserve human approval before an application is actually submitted.

## 8. Current engineering roadmap

| Workstream | State | Approx. progress |
| --- | --- | ---: |
| Goals & success criteria | Complete | 100% |
| System architecture | Local modular monolith implemented; hosted runtime planned | 75% |
| Data contracts / schemas | Core account, profile, evidence, job, evaluation, workflow, and history models implemented | 75% |
| Scoring engine | Profile-driven deterministic v2 with versioned fixtures | 70% |
| Persistence | PostgreSQL/EF Core migrations and owner-scoped repositories implemented | 75% |
| Application workflow | Database-owned lifecycle and dashboard implemented | 80% |
| Hosted daily-use beta | Planned on a zero-subscription stack | 10% |
| Resume tailoring | Not started | 0% |
| Browser automation | Deferred | 0% |

## 9. Immediate v0.1 milestone

The local workflow now supports accounts, editable candidate evidence, job intake, profile-driven scoring, versioned evaluation history, database-owned application state, outcomes, follow-ups, and dashboard metrics. The immediate milestone is to make this workflow dependable enough for daily personal use from desktop and mobile.

The first hosted beta will use a free application host and managed free PostgreSQL. It will remain private and single-user, accept cold starts during the pilot, and require HTTPS, closed registration, durable database state, persistent session keys, structured diagnostics, and a tested backup restore. The Angular client will be installable as a standalone PWA without caching authenticated API data.

The complete execution plan is maintained in [`hosted-beta-plan.md`](hosted-beta-plan.md). Model-assisted semantic scoring comes later, after daily use produces a useful set of review decisions and exposes the real workflow bottlenecks.

The ordered delivery plan is maintained in [`roadmap.md`](roadmap.md).

## 10. Near-term design questions

These are intentionally unresolved and should be decided from pilot evidence rather than assumed:

- Which resume input should the reviewed evidence importer support first: PDF, DOCX, or pasted text?
- What feedback taxonomy provides useful learning without making daily tracking burdensome?
- Which deterministic filters should become configurable after real false-positive and false-negative examples exist?
- When do free-hosting cold starts or quotas justify a paid service?
- What labeled review set and improvement threshold should semantic scoring have to beat before affecting recommendations?

## 11. Guiding constraint

Every major feature should answer at least one of these questions:

- Does this improve the real job search?
- Does this make the system more correct, measurable, maintainable, or explainable?

If it does neither, it probably does not belong in v0.1.
