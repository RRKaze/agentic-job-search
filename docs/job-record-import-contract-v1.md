# Job record import contract v1

Status: implementation specification; API and workflow are not implemented by this document.

## Temporary bridge and eventual cutover

GitHub is authoritative only during this bridge phase. The database will become the source of truth; the scheduled search and dashboard will then write through application APIs. No database-to-GitHub write-back is planned. At cutover, pause source writers, verify a final import, switch writers to the API, and disable this import capability and workflow. Keep core records and local feedback independent of the importer.

## Scope and ownership

Import the complete current `data/jobs.csv` and `data/applications.csv` snapshot from the private `RRKaze/job-search` repository, branch `main`, into the application's PostgreSQL database. GitHub owns all imported fields. No resume contents, search-profile data, scoring recalculation, application submissions, GitHub write-back, or deletion is included in v1. Import does not require AI.

Imported fields are read-only in the dashboard for this milestone. Local feedback is stored separately and is never overwritten by imports. Do not conflate imported career stages with internal evaluation lifecycle states.

## Endpoints and authentication

- `GET /api/v1/imports/job-records/checkpoint`: returns `{ "source_commit": null }` before the first import, otherwise the last successfully imported full commit SHA.
- `POST /api/v1/imports/job-records`: validates or applies one snapshot.
- Both require `Authorization: Bearer <import-token>`. The token is scoped to this configured repository/import capability, stored in backend configuration and GitHub Actions secrets, and never shipped to the browser.
- POST requires `Content-Type: application/json`. Hosted connections require HTTPS; HTTP is permitted only inside the isolated integration-test network or local loopback development.
- v1 accepts at most 5 MiB of request body and 10,000 records in each array. Reject larger requests with 413, never silently truncate.

## Request shape

```json
{
  "schema_version": "1.0",
  "dry_run": true,
  "source": {
    "repository": "RRKaze/job-search",
    "branch": "main",
    "commit_sha": "1111111111111111111111111111111111111111",
    "expected_previous_commit": null
  },
  "jobs": [],
  "applications": []
}
```

The SHA above is illustrative. All properties are required, including `dry_run`; omission is an error, never implicit permission to write. `expected_previous_commit` is copied from the checkpoint response. Both arrays contain the complete snapshots from the same checked-out commit. Empty arrays are valid only for a genuinely empty source; omissions of previously imported IDs are rejected as described below.

Payload property names match CSV headers exactly. Each row contains every listed property. Required values are nonblank strings; optional empty CSV cells become JSON `null`. Preserve nonempty text, including multiline notes. Dates are strings in real calendar `YYYY-MM-DD` form; never invent dates or time zones. Reject unknown properties and unsupported schema versions. IDs are opaque case-sensitive strings, not GUIDs; never renumber, case-fold, or regenerate them.

### Job row

| Property | Value |
| --- | --- |
| job_id | Required permanent ID |
| company | Required text |
| role | Required text; maps to application title |
| location | Required text |
| salary_text | Optional text, no numeric conversion |
| priority | Required text; preserve source vocabulary |
| fit_rationale | Optional text |
| gaps_notes | Optional text |
| stage | Required: lead, applied, interviewing, offer, rejected, withdrawn, unavailable, ineligible |
| status_date | Optional date |
| source_url | Required absolute HTTPS URL with hostname and no embedded credentials |
| verified_date | Required date |

### Application row

| Property | Value |
| --- | --- |
| application_id | Required permanent ID |
| job_id | Required ID of a job in this snapshot |
| submitted_date | Required date |
| status | Required: submitted, interviewing, offer, rejected, withdrawn |
| resume_version | Optional text reference, not resume bytes |
| referral_contact | Optional text; personal data |
| next_follow_up | Optional date |
| outcome | Optional text, subject to rejected rule below |
| notes | Optional text; personal data |

Field limits: IDs 128 characters; company/role/location 500; URL 2048; priority 200; salary/resume reference/contact/outcome 2000; rationale/gaps/notes 20000. Exceeding a limit fails validation; no truncation. Existing database column limits must be migrated to support the contract before enabling imports.

## Validation and status mapping

- Enforce unique job_id and application_id, and at most one application per job. Reject exact duplicate company-role pairs, matching the current repository validator; potential case/spacing variants may be reported separately but must not be silently merged.
- A previously imported application_id cannot change its linked job_id.
- Every application must link to a job in the submitted snapshot and its status must match the job stage:

| Application status | Job stage | Dashboard label |
| --- | --- | --- |
| submitted | applied | Applied |
| interviewing | interviewing | Interviewing |
| offer | offer | Offer |
| rejected | rejected | Rejected |
| withdrawn | withdrawn | Withdrawn |

- Jobs without application rows remain jobs only. Do not synthesize an application, even if the source job stage suggests a historical submission. Show a nonblocking missing-application warning for applied/interviewing/offer jobs without an application row.
- Map lead to Saved, unavailable to Unavailable, and ineligible to Ineligible for display; retain the original source stage. Rejected or withdrawn jobs may exist without application rows.
- Rejected applications require outcome `rejected` and a null next_follow_up, matching current source validation. Also require null next_follow_up for withdrawn applications. Do not treat offer as closed automatically.
- The server validates independently of the repository validator. Its stricter checks (real dates, nonblank application IDs, URL parsing, limits, withdrawn follow-up) must be documented by the Action; it must report incompatible rows without changing them.
- URLs are stored, not fetched by the importer. The API cannot establish that a URL is an official employer page merely from syntax.

## Atomicity, freshness, and retries

The backend must verify that source.commit_sha is the current configured main-branch head using read-only GitHub access before accepting either dry-run or apply. Fail closed if verification is unavailable. Do not trust commit timestamps, SHA lexical order, or only the caller's declaration. Reject a stale head with 409. Integration tests use a dependency-injected source verifier with controlled fixtures; no HTTP request parameter may bypass verification on a deployed endpoint.

Apply runs in a transaction serialized per source repository/branch. Recheck the database checkpoint under that lock. If expected_previous_commit differs from the checkpoint, return 409 and require the caller to reread the checkpoint and latest source. Do not automatically replay a conflicting payload. A later GitHub commit arriving after head verification is handled by the next import; an older in-flight request cannot overwrite a newer committed import.

Store a deterministic payload hash alongside the checkpoint. Hash normalized schema_version, repository, branch, commit_sha, jobs sorted by job_id and applications sorted by application_id, with stable property order and UTF-8 serialization. Exclude dry_run and expected_previous_commit. The server owns hash calculation.

If the submitted SHA equals the current checkpoint and the hash matches, return `already_imported` without writes (or `validated` for dry-run), regardless of the old expected_previous_commit. If the same SHA has a different hash, return 409 `SOURCE_CONTENT_CONFLICT`. A retry for a SHA that is no longer the main head returns 409 `STALE_SOURCE` instead. This defines retry behavior without duplicate history.

Use unique keys (source repository, external ID) for jobs/applications while retaining internal database GUIDs. Validate all rows first. Apply all row changes and the checkpoint in one transaction. Null values explicitly clear optional imported fields. Missing rows never mean delete: reject snapshots omitting any previously imported ID with 409 `SOURCE_RECORD_MISSING`, preserving existing data. Deletion/archival requires a later explicit contract.

Do not reset created dates on updates. Record actual imported stage/status changes with old value, new value, source commit and import time. The first import records an initial observation, not invented historical transitions. An import timestamp is distinct from source status_date/submitted_date.

Dry-run performs the same validation, source verification and checkpoint checks, computes proposed counts, and writes no jobs, applications, status history, or checkpoint. Its result is advisory, not a reservation; apply revalidates everything.

## Responses

Successful validation or apply returns HTTP 200:

```json
{
  "schema_version": "1.0",
  "result": "validated",
  "source_commit": "1111111111111111111111111111111111111111",
  "dry_run": true,
  "counts": {
    "jobs": { "inserted": 2, "updated": 0, "unchanged": 0 },
    "applications": { "inserted": 1, "updated": 0, "unchanged": 0 }
  },
  "warnings": []
}
```

Allowed result values: `validated`, `imported`, `already_imported`. Dry-run counts describe proposed changes. Already-imported counts are all unchanged. A successfully applied new commit with unchanged rows still advances the checkpoint.

Error envelope:

```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Import rejected; no records were changed.",
    "details": [
      { "entity": "applications", "row": 1, "field": "status", "code": "STATUS_MISMATCH" }
    ]
  }
}
```

Rows are 1-based array positions, not CSV line numbers (notes may span lines). Return at most 100 details plus `truncated: true` if necessary. Warnings use the same location/code shape. No field values, contacts, notes, credentials or payloads in responses/logs. Infrastructure failures need not use the JSON envelope; clients must handle non-JSON failures safely.

Status codes: 400 malformed request or unsupported version; 401 missing/invalid token; 403 unauthorized source; 409 source/checkpoint/content/missing-record conflict; 413 size limit; 422 invalid records; 503 source verification/database unavailable; 500 unexpected failure. Any non-200 response fails the workflow. On connection loss the commit outcome may be unknown: report uncertainty and retry the identical payload safely, rather than claiming no change occurred.

## GitHub Action handoff

Use workflow_dispatch only for v1. Inputs: mode (`integration_test` default, or `configured_endpoint`) and dry_run (default true). Only main is accepted for persistent imports; never expose arbitrary target URLs or secrets as workflow inputs. Configure IMPORT_API_BASE_URL as a repository variable and IMPORT_API_TOKEN as a secret. Backend GitHub read credentials are separate from this token.

1. Check out main and resolve the actual full commit SHA; do not assume github.sha is the chosen source snapshot.
2. Run scripts/validate_records.py. Convert both CSV files at that checkout into the exact JSON contract using a CSV parser, preserving quoted multiline text.
3. Read the checkpoint, then submit one snapshot. Report counts and safe error locations only. Do not upload real-record payloads as artifacts.
4. Serialize import workflow runs without cancelling an in-progress write. Server-side concurrency checks remain mandatory.
5. Integration mode starts a pinned API artifact and disposable PostgreSQL, supplies a test-only verifier, runs dry-run, apply and repeat-apply, and verifies dry-run wrote nothing and repeated imports created no duplicates. Fixture tests separately cover invalid records, rollback, missing IDs, stale commits, concurrent checkpoint conflicts, and mismatched content for the same SHA. Pin/version provenance of the API artifact must be documented before implementation; the Action must not guess its location.
6. Configured-endpoint mode requires a reachable authenticated endpoint. It cannot reach a laptop's localhost. On 409 require inspection/fresh source; do not blindly retry with an updated checkpoint. On transport/503 failures use bounded retries of the identical request.

If future automation commits with a GitHub Actions GITHUB_TOKEN, do not assume that push will trigger another workflow; automatic triggering remains a separate future design. Manual dispatch is the only trigger promised by this contract.

## Ownership of implementation

Agentic Job Search project: database migrations, endpoint/authentication, source verification, transactional import service, tests, and pinned runnable test artifact.

Job Search project: CSV conversion, manual workflow, secret/variable documentation, and integration against this contract. Neither project changes the other's canonical data as part of implementation. This document is a handoff artifact; it has not been installed or committed in either repository.
