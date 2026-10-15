# Live ATS verification checkpoint — 2026-10-15

Scope: API-only; Playwright deliberately deferred. The user's V1 baseline remains frozen; do not count tests as Apply leads.

| Provider | Endpoint tested | Result | Interpretation |
|---|---|---|---|
| Greenhouse | https://boards-api.greenhouse.io/v1/boards/greenhouse/jobs | HTTP-readable JSON, 18 records, titles/IDs/locations | Public API confirmed accessible via web retrieval |
| Ashby | https://api.ashbyhq.com/posting-api/job-board/Horizon3ai?includeCompensation=true | retrieval tool reported inaccessible | Not a provider failure; no live validation yet |
| Lever | https://api.lever.co/v0/postings/netflix?mode=json | HTTP 404 | Invalid/unconfirmed test board; inconclusive |

The execution container has no outbound DNS/network access to these endpoints, so the committed Python adapter and its unit tests were not executed against live endpoints. Greenhouse web retrieval proves the public endpoint returns JSON, not that the local script runs correctly.

Next:
1. Add offline CI test workflow and run tests in GitHub Actions.
2. Test Ashby against a confirmed public board and Lever against a confirmed active board, recording status and response shape.
3. Reconcile API results with existing jobs.csv and audit history; preserve Hold for unknown compensation, unsupported geography, or missing work arrangement.
4. Only then run V2 discovery, keeping new assessments and provisional leads distinct from confirmed Apply entries.
