# Temporary import bridge: application-side handoff

GitHub is authoritative only until the planned database cutover. Do not build write-back to GitHub. The API contract is in `docs/job-record-import-contract-v1.md`.

## Run the real API locally

Use .NET 9 and PostgreSQL 16. Set `ConnectionStrings__JobSearch`, `Imports__Token` (a random private import credential), and `Imports__GitHubReadToken` (read-only access to RRKaze/job-search). Run `dotnet run --project src/AgenticJobSearch.Api`. Startup applies additive EF migrations; back up any persistent database before migration. Missing import credentials disable access; missing GitHub read credentials cause source verification to fail closed.

The existing application routes have no user authentication yet. This milestone is for local/disposable testing, not public deployment of personal data. Production deployment needs user authentication and HTTPS at the application boundary. Do not expose PostgreSQL publicly for this integration.

Configure the caller with the API origin as `IMPORT_API_BASE_URL` (no `/api` suffix) and store its bearer token in `IMPORT_API_TOKEN`. Read GET `/api/v1/imports/job-records/checkpoint`, then POST `/api/v1/imports/job-records`. Responses are snake_case. The server strictly checks required JSON keys, duplicate keys, calendar dates, IDs, references, status agreement, size, and freshness.

Imported fields are stored as typed tracking columns on Jobs and Applications; the canonical source-row JSON is retained for change detection. Submission dates are stored as PostgreSQL dates, separate from the existing timestamp field. Core GUIDs remain internal. TrackingStage deliberately does not modify the scoring lifecycle. No applications are created for leads. Feedback is untouched. History records observed changes, not reconstructed historical events.

## Disposable GitHub runner test

Check out this repository at a pinned commit (the branch's delivered commit SHA), then build:

```sh
docker build --platform linux/amd64 -f Dockerfile.import --target integration-test -t job-import-test .
```

Start PostgreSQL 16 on a private Docker network, wait for `pg_isready`, then start that image on the same network with:

- `ConnectionStrings__JobSearch`: connection to the disposable database.
- `Imports__Token`: a newly generated test-only token.
- `TestSource__Head`: the exact 40-character SHA used in the request.

The verified artifact targets Linux AMD64, matching standard GitHub Ubuntu runners. The local ARM build failed inside the .NET runtime; use the explicit platform above when reproducing on Apple Silicon.

For a self-contained synthetic HTTP check after building, run `python3 scripts/import-smoke-test.py`. It creates and removes its own database, API container, network, and anonymous volume.

The image listens on port 8080. Its ImportIntegration environment is set in the image. Poll `/api/health` with a bounded timeout, then use the normal authenticated import endpoints. Do not assume an immediate container start means the API is ready; migrations must complete first.

This separate test executable injects a fixed source-head verifier. The production executable has no configuration switch or request parameter that enables this verifier. The production Docker target excludes the test executable. To test a new source commit, recreate the test API container with its new fixture head, preserving the disposable database. Use synthetic records for deterministic failure tests; real private CSV payloads must never appear in logs or artifacts. Destroy test containers/volumes at the end.

At minimum verify: dry-run counts without checkpoint changes, successful apply, identical retry returns already_imported, invalid import returns 422 without advancing checkpoint, and stale checkpoint returns 409. The app's PostgreSQL integration suite covers the daily lifecycle and persistence invariants as well.

No registry image is published by this change. The runnable artifact is the Docker build target at a pinned source commit. Never substitute a floating branch name for the pin in the final Action.

## Failure and recovery

On validation/conflict failures, inspect safe error codes and prepare a fresh snapshot. On a transport failure, outcome may be unknown; retry the identical request. Server checkpoint and content hashing prevent duplicate work. Import histories/checkpoint update in the same transaction as rows. Removed IDs are conflicts, never deletes. Dry-run is advisory; apply repeats all checks.

At database cutover: pause source writers, apply and verify the final import, switch discovery and status writers to application APIs, disable the import token/Action, and keep GitHub as historical evidence. Local feedback and core records survive importer retirement.
