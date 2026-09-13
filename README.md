# Agentic Job Search

A stateful, measurable job-search workflow built to improve a real Senior Software Engineer search while serving as a serious software-engineering project.

## Why this exists

Job searching is usually fragmented across job boards, spreadsheets, resume copies, notes, and memory. This project aims to turn that process into an inspectable workflow that can discover and evaluate roles, track applications and outcomes, learn from feedback, and eventually assist with evidence-grounded resume tailoring.

The goal is not to build an LLM wrapper or blindly automate applications. The system should make better decisions, preserve state, measure outcomes, and keep a human in control.

## v0.1 direction

The first version focuses on the foundations:

- explicit domain models for `CandidateProfile`, `Job`, `Application`, and `Feedback`
- a workflow/state-machine approach instead of one large prompt
- hard filters + deterministic scoring + LLM semantic scoring
- separate **job fit** from **application priority**
- SQLite-backed persistence
- feedback and application outcomes as first-class data
- evidence-grounded resume tailoring

Browser/application automation comes later, after scoring and tracking are reliable.

## Current status

| Area | Status |
| --- | --- |
| Goals & success criteria | Complete |
| System architecture | In progress (~25%) |
| Data contracts / schemas | Not started |
| Scoring engine | Not started |
| Persistence | Not started |
| Resume tailoring | Not started |
| Browser automation | Deferred / low priority |

### Current milestone

Define the v0.1 system boundaries and data flow, then define the initial contracts for `CandidateProfile`, `Job`, `Application`, and `Feedback` before substantial implementation begins.

## Design principles

The project favors explicit state, deterministic behavior where possible, measurable outcomes, small reviewable milestones, and infrastructure only when justified by actual requirements.

Resume/candidate claims must be backed by verified evidence. The system must not fabricate technologies, experience, ownership, metrics, scope, or outcomes to improve apparent job fit.

## Repository guidance

Coding agents should read [`AGENTS.md`](AGENTS.md) before making changes. More detailed project state and decisions are tracked in [`docs/project-context.md`](docs/project-context.md).

## Success criteria

This project succeeds only if it does both:

1. materially improves the efficiency and outcomes of the real job search; and
2. reaches enough engineering depth and quality to be credibly showcased as a public Senior Software Engineer project.
