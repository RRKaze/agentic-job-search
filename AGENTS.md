# Agentic Job Search — Codex Project Instructions

This file is the canonical working context for Codex and other coding agents operating in this repository.

## Project Purpose
Build a stateful, measurable job-search system that materially improves a real Senior Software Engineer job search and reaches sufficient engineering quality to be publicly showcased on GitHub and potentially included on a Senior SWE resume. This must become a genuine software system, not an LLM wrapper. Avoid unnecessary over-engineering.

## Project Boundary
There are two distinct projects:
1. **Job Search** — resume development, job discovery/scoring, applications, interview preparation, outcomes, and feedback-driven iteration.
2. **Agentic Job Search Workflow** — this repository. It implements software that consumes/synchronizes verified outputs and feedback from Job Search.

Do not collapse these projects. Job Search remains the source of truth for real-world career decisions and verified candidate evidence.

## Candidate Positioning
- Primary target: Senior Software Engineer.
- Best-fit families: Senior Backend, Platform, Identity/Authentication, Distributed Systems.
- Selective backup: SWE III / mid-senior when fit is strong.
- Staff/Principal is not a primary target.
- Penalize unusually heavy support/on-call roles.
- U.S. citizen; sponsorship is not required.

## Evidence Safety Rule
Resume tailoring must use verified evidence only. Never invent or inflate technologies, ownership, scope, scale, metrics, architectural mechanisms, or business outcomes. When evidence is uncertain, preserve the uncertainty or omit the claim.

Example: transactional rollback/recovery semantics may be described when verified, but do not claim a specific `.NET TransactionScope` implementation unless explicitly confirmed.

## Verified Candidate Evidence
- Senior Software Engineer at UKG, Jan 2022–present.
- Production C#, .NET / ASP.NET Core, REST APIs, SQL.
- Production React / TypeScript / JavaScript; Python experience/project use.
- Authentication modernization/migration supporting 1,000+ enterprise tenants/companies.
- Migration flow includes staged readiness checks, remediation, asynchronous synchronization, validation, and feature-flag recovery.
- Producer/consumer async workflows with selective retry for transient failures and remediation paths for data-validation failures.
- User-level transactional processing; incomplete operations can roll back on timeout while preserving migration recovery semantics.
- Authentication service participates as producer and consumer in an internally built/self-hosted messaging/eventing system.
- Backend C# workers/processors pick up enqueued jobs and perform data processing/cleaning with mixed async/sync flows.
- Deep production/system/data diagnosis across authentication migration workflows, targeted remediation, and systemic follow-up.
- Product/design partnership working backward from desired outcomes to identify technical gaps, dependencies, edge cases, and timeline tradeoffs.
- SRE/production incident and customer escalation experience.
- TeamCity CI/CD automation; prior ~40% deployment/operational-effort improvement claim must only be used if evidence remains supportable.
- Cross-team collaboration at 20+ team scale appeared in prior resume material; use cautiously and truthfully.
- Mentoring/onboarding/documentation are supporting evidence, not core positioning.

## Architecture Principles
Design this as a workflow/state machine rather than one giant prompt.

Core domain concepts should include at minimum:
- CandidateProfile
- Job
- Application
- Feedback

Scoring should combine hard filters, deterministic scoring, and LLM semantic scoring. Keep **fit score** separate from **application priority**.

Start persistence with SQLite. Consider PostgreSQL only if requirements justify it.

Browser/application automation is intentionally low priority. Build scoring, state tracking, and feedback loops first. Keep human approval before application submission.

## Current Engineering Progress
- Goals & success criteria — Complete
- System architecture — In Progress (~25%)
- Data contracts/schemas — Not Started
- Scoring engine — Not Started
- Persistence — Not Started
- Resume tailoring — Not Started
- Browser automation — Not Started; intentionally low priority

## Immediate Milestone
Define v0.1 system boundaries and data flow, then define schemas/contracts for CandidateProfile, Job, Application, and Feedback before implementing substantial application code.

## Working Rules for Codex
1. Read this file first.
2. Preserve the separation between Job Search state and this engineering project.
3. Prefer explicit domain models and inspectable state over hidden prompt state.
4. Optimize for measurable usefulness in the real job search.
5. Make architecture resume-worthy only when it solves real system needs.
6. Avoid infrastructure before requirements justify it.
7. Add tests for deterministic logic, especially scoring and state transitions.
8. Prefer small, reviewable milestones over speculative scaffolding.
9. Document material architectural decisions and changes to assumptions.
10. Never generate candidate claims unsupported by verified evidence.

## Current Next Action
Start with the v0.1 architecture/data-contract design. Implementation should emerge from those contracts rather than ad-hoc prompt orchestration.
