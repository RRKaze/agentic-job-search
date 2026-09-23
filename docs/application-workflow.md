# Database-owned application workflow

PostgreSQL now owns job and application status. The former GitHub snapshot importer is disabled by default so an old CSV snapshot cannot overwrite changes made in the application. `Imports:Enabled=true` is reserved for isolated integration tests and deliberate recovery; it must not be enabled for the active workspace.

## Statuses and transitions

The canonical statuses are Saved (`lead`), Applied, Interviewing, Offer, Rejected, and Withdrawn. Saved jobs can move to Applied, which creates their application record. Existing applications use these validated transitions:

| Current | Allowed next statuses |
| --- | --- |
| Applied | Interviewing, Rejected, Withdrawn |
| Interviewing | Applied, Offer, Rejected, Withdrawn |
| Offer | Interviewing, Rejected, Withdrawn |
| Rejected | Applied, Interviewing |
| Withdrawn | Applied |

Backward transitions support correction and reopening while preserving application notes. Applications do not move back to Saved because removing the application would discard workflow context. Updating details without changing status is allowed and does not create a duplicate status-history entry.

Each update stores the status date, submitted date, resume version, next follow-up, outcome or next action, and notes. Rejected and withdrawn applications cannot retain a follow-up date. Status changes are recorded with the account, previous and current status, job, and timestamp.

## API and ownership

- `PUT /api/jobs/{id}/workflow` validates and persists one owner-scoped update.
- `GET /api/jobs/{id}/workflow-history` returns that account's status history for the job.
- Updating a legacy job through the explicitly configured legacy owner account claims that job for the account.
- Another account receives `404` when attempting to update the job and receives no history.

The frontend replaces the updated job in its local store. Moving a Saved job to Applied therefore removes it from Opportunities and adds it to Applications immediately. Status-filtered lists also update without a refresh.
