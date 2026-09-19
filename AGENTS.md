# Agentic Job Search — Codex Project Instructions

This file is the canonical working context for Codex and other coding agents operating in this repository.

## Project Purpose
Build a stateful, measurable job-search system that materially improves a real Senior Software Engineer job search and reaches sufficient engineering quality to be publicly showcased on GitHub and potentially included on a Senior SWE resume. This must become a genuine software system, not an LLM wrapper. Avoid unnecessary over-engineering.

## Project Boundary
There are two distinct projects:
1. **Job Search** — resume development, job discovery/scoring, applications, interview preparation, outcomes, and feedback-driven iteration.
2. **Agentic Job Search Workflow** — this repository. It implements software that consumes/synchronizes verified outputs and feedback from Job Search.

Do not collapse these projects. Job Search remains the source of truth for real-world career decisions and verified candidate evidence.

## Demo Candidate Defaults
The checked-in candidate profile is fictional public demo data. Its defaults target Senior Software Engineer roles in backend, platform, identity/authentication, and distributed systems. It treats unusually heavy support/on-call expectations as a negative ranking factor.

Real candidate identity, employment history, authorization status, preferences, and evidence must be supplied outside the public seed data. Do not commit personal candidate data without explicit approval.

## Evidence Safety Rule
Resume tailoring must use verified evidence only. Never invent or inflate technologies, ownership, scope, scale, metrics, architectural mechanisms, or business outcomes. When evidence is uncertain, preserve the uncertainty or omit the claim.

Example: transactional rollback/recovery semantics may be described when verified, but do not claim a specific `.NET TransactionScope` implementation unless explicitly confirmed.

## Fictional Demo Evidence
The public seed may use only clearly fictional, non-identifying statements such as:

- backend development with C#, .NET / ASP.NET Core, REST APIs, and SQL
- frontend experience with React, TypeScript, and JavaScript
- authentication and authorization workflow experience
- producer/consumer processing, retry handling, and data validation
- production diagnosis, incident response, and customer support
- product and design collaboration around requirements and tradeoffs

This fixture exists to demonstrate product behavior. It must never be presented as a real person's resume or employment record.

## Architecture Principles
Design this as a workflow/state machine rather than one giant prompt.

Core domain concepts should include at minimum:
- CandidateProfile
- Job
- Application
- Feedback

Scoring should combine hard filters, deterministic scoring, and LLM semantic scoring. Keep **fit score** separate from **application priority**.

Use PostgreSQL with EF Core for v0.1 because the current implementation direction prioritizes enterprise-transferable tooling and a path toward a generalized multi-user product. Avoid adding heavier infrastructure until requirements justify it.

Browser/application automation is intentionally low priority. Build scoring, state tracking, and feedback loops first. Keep human approval before application submission.

## Current Engineering Progress
- Goals & success criteria — Complete
- System architecture — In Progress
- Data contracts/schemas — Started
- Scoring engine — Started
- Persistence — Started
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
Continue the v0.1 vertical slice from the implemented manual job intake path: improve API/UI usability, add persistence migrations, and keep deterministic scoring covered by tests before adding LLM-assisted evaluation.
