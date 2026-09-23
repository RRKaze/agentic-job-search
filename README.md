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
- PostgreSQL-backed persistence through EF Core
- feedback and application outcomes as first-class data
- evidence-grounded resume tailoring

Browser/application automation comes later, after scoring and tracking are reliable.

## Current status

| Area | Status |
| --- | --- |
| Goals & success criteria | Complete |
| System architecture | In progress (~40%) |
| Data contracts / schemas | Initial models implemented |
| Scoring engine | Deterministic V0.1 implemented |
| Persistence | PostgreSQL/EF Core implemented |
| Resume tailoring | Not started |
| Browser automation | Deferred / low priority |

### Current milestone

Harden the implemented manual intake slice, then add an editable, evidence-backed candidate profile. See [`docs/roadmap.md`](docs/roadmap.md) for the ordered plan.

## Run locally

The first implemented vertical slice is split into an ASP.NET Core API and an Angular frontend.

### Prerequisites

Install and start:

- Docker Desktop
- .NET 9 SDK
- Node.js 22.22.3 LTS with npm (the version in `.nvmrc`)

Clone the repository and enter its root directory before running the commands below.

### First-time setup

Install the frontend packages:

```bash
cd apps/web
npm install
cd ../..
```

The .NET packages restore automatically when the API is first run. You can also restore them explicitly with `dotnet restore AgenticJobSearch.sln`.

If your local database was created before EF Core migrations were introduced, reset the development volume once before starting this version:

```bash
docker compose down --volumes
```

This deletes local development data. It is not required for a fresh clone or for databases already managed by migrations.

### Start the application

Use three terminal windows and leave the API and frontend processes running while you use the application.

1. From the repository root, start PostgreSQL and the database viewer:

   ```bash
   docker compose up -d postgres adminer
   ```

2. From the repository root, start the API:

   ```bash
   dotnet run --project src/AgenticJobSearch.Api
   ```

   The API listens on `http://localhost:5156`.

   The API applies pending EF Core migrations during startup.

3. From `apps/web`, start the Angular app:

   ```bash
   cd apps/web
   npm start
   ```

Open these local URLs:

| Service | URL | Purpose |
| --- | --- | --- |
| Application | http://localhost:4200 | Add, score, and review jobs |
| API health | http://localhost:5156/api/health | Confirm the backend is responding |
| Saved jobs API | http://localhost:5156/api/jobs | Inspect saved job JSON |
| Database viewer | http://localhost:8080 | Browse PostgreSQL tables and rows |

### View database data

Open Adminer at `http://localhost:8080` and sign in with these local-development values:

| Field | Value |
| --- | --- |
| System | PostgreSQL |
| Server | `postgres` |
| Username | `agentic` |
| Password | `agentic` |
| Database | `agentic_job_search` |

Select a table and choose **Select data**. The most useful tables are `Jobs`, `JobEvaluations`, `JobEvaluationFactors`, `CandidateProfiles`, and `CandidateEvidence`.

These credentials are only for local development. Production credentials must be supplied through secure configuration and must not be committed.

### Stop the application

1. Press `Ctrl+C` in the Angular terminal.
2. Press `Ctrl+C` in the API terminal.
3. From the repository root, stop the Docker services:

   ```bash
   docker compose down
   ```

`docker compose down` preserves the PostgreSQL data volume. The next startup will retain saved jobs.

To deliberately delete all local database data and start fresh, run:

```bash
docker compose down --volumes
```

This reset command is destructive and cannot recover the deleted local data.

## Basic usage

1. Open `http://localhost:4200`.
2. Enter a title, company, location, and source URL when available.
3. Paste the complete job description. This is the only required field.
4. Submit the form.
5. Review eligibility, fit score, application priority, recommendation, explanation, and scoring factors.
6. Select earlier jobs from the recent-jobs list to compare results.

The current scorer is deterministic and inspectable. It does not yet call an LLM or tailor resumes.

## Verify the project

Run backend tests from the repository root:

```bash
dotnet test tests/AgenticJobSearch.Tests
```

Run PostgreSQL-backed API integration tests while Docker Desktop is running:

```bash
dotnet test tests/AgenticJobSearch.Api.IntegrationTests
```

The integration tests create and remove their own disposable PostgreSQL container. They do not use the local development database.

Build the frontend from `apps/web`:

```bash
npm run build
```

## Troubleshooting

If the UI says it cannot load jobs:

- Confirm the API terminal says `Now listening on: http://localhost:5156`.
- Open `http://localhost:5156/api/health` and confirm it returns a JSON response.
- Confirm PostgreSQL is healthy with `docker compose ps`.

If Docker reports that port `5432`, `8080`, `5156`, or `4200` is already in use, stop the other process using that port before starting this project.

The Angular CLI does not support Node 23. Use the version in `.nvmrc` with a Node version manager, or install a supported version listed in `apps/web/package.json`.

Production dependencies are checked in CI with `npm audit --omit=dev --audit-level=high`. The current audit status and upgrade decision are documented in [`docs/dependency-security.md`](docs/dependency-security.md).

## Database migrations

The repository pins the EF Core CLI as a local .NET tool. Restore it once after cloning:

```bash
dotnet tool restore
```

After changing the EF Core model, create and review a migration before committing:

```bash
dotnet tool run dotnet-ef migrations add MigrationName \
  --project src/AgenticJobSearch.Infrastructure \
  --startup-project src/AgenticJobSearch.Api \
  --output-dir Persistence/Migrations
```

Application startup applies pending migrations. Production deployment policy may move migration execution into a dedicated release step as the deployment model matures.

## Design principles

The project favors explicit state, deterministic behavior where possible, measurable outcomes, small reviewable milestones, and infrastructure only when justified by actual requirements.

Resume/candidate claims must be backed by verified evidence. The system must not fabricate technologies, experience, ownership, metrics, scope, or outcomes to improve apparent job fit.

## Repository guidance

Coding agents should read [`AGENTS.md`](AGENTS.md) before making changes. More detailed project state and decisions are tracked in [`docs/project-context.md`](docs/project-context.md).

## Success criteria

This project succeeds only if it does both:

1. materially improves the efficiency and outcomes of the real job search; and
2. reaches enough engineering depth and quality to be credibly showcased as a public Senior Software Engineer project.


## Temporary GitHub record import

The manual-import API and disposable runner test artifact are described in [the import handoff](docs/import-api-handoff.md) and [v1 contract](docs/job-record-import-contract-v1.md). This is a temporary GitHub-to-database bridge; database-to-GitHub write-back is intentionally excluded. The dashboard integration and hosting are separate milestones.

## User accounts

Register or sign in at the dashboard URL. First-time users choose **New graduate** or **Experienced worker** before entering the workspace. Use **Personal profile** in the navigation to view and update this information. See [account setup and existing workspace ownership](docs/accounts.md) for local proxy configuration and the explicit legacy-data owner assignment.

## Application workflow

Saved opportunities can now move directly into Applications. Application rows provide validated status changes, while the detail panel stores status and submission dates, resume version, follow-up, outcome, and notes. PostgreSQL owns these edits and records status history; the temporary GitHub importer is retired by default. See [database-owned application workflow](docs/application-workflow.md) for transitions and API behavior.
