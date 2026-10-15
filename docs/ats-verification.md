# API-only ATS verification and V2 discovery

These standard-library Python tools do not use Playwright, submit applications,
send candidate information, or write canonical job/application records. The V2
integration is an explicit CLI review workflow; the ASP.NET Worker is unchanged.

## Verification

```sh
python3 scripts/verify_ats.py 'https://jobs.ashbyhq.com/BOARD/POSTING_ID' --json
python3 -m unittest discover -s tests -p 'test*.py' -v
python3 scripts/smoke_ats.py  # explicit public-network test
```

- `published` (exit 0): posting present on a valid public board.
- `not_found` (exit 2): valid board returned without the ID; not proof of closure.
- `unverified` (exit 3): request/schema/identity error or unsupported URL.

Ashby derives IDs from canonical job/application URLs, falling back to `id` only
when URLs are absent. This supports omitted IDs and internal IDs that differ from
posting IDs. URL identities must agree with the board. Slugs are lowercased.
Lever maps `hostedUrl` and `applyUrl` separately, extracts structured salary and
supports US/EU hosts. Greenhouse content is decoded before removing HTML tags.
Descriptions, compensation evidence, arrangement and verification timestamps are
retained. Missing compensation is never inferred from title or employer.

Requests use allowlisted public API hosts, GET/HTTPS only, no credentials or
redirects, a 15-second timeout and 25 MB response cap. Malformed records invalidate
the board conservatively. Missing `jobs` is not an empty board. Greenhouse total
metadata, when supplied, must match the returned count.

## V2 retrieval and review

Keep real manifests and outputs outside this engineering repository. Use the
latest `RRKaze/job-search` checkout as `--canonical`. A manifest is a JSON array:

```json
[{"provider":"ashby","board":"example","company":"Example",
  "bucket":"remote","family":"employer-first","query":"Example public board"}]
```

The example is fictional. Optional `posting_url` and `role` target one posting;
absent/unverifiable postings then remain Hold with their tri-state status.
Otherwise the title filter creates a broad queue, not a count of assessed jobs.

```sh
python3 scripts/discover_v2.py --manifest /private/path/sources.json \
  --canonical /path/to/job-search --output /private/path/new-run
python3 scripts/discover_v2.py --canonical /path/to/job-search \
  --output /private/path/new-run --reviews /private/path/decisions.json
```

Outputs: `retrieval.json`, `boards.json`, `seen-ledger.json`, and (after review)
`audit.csv`. One request per board per invocation; repeated source families reuse
it. The ledger derives first_seen/last_checked from historical audits. Dedup uses
normalized ATS URL identities, tracking-free URLs, canonical employer+role and
prior employer+role+location. Generic employer URL aliases also need reviewer
checks; there is no fuzzy semantic matching.

Decision JSON objects require `source_url`, `decision` (Apply/Hold/Skip),
`primary_reason`, `lead_bucket`, and `notes`. Salary/location evidence from the
body may be transcribed with `salary_text` and `location`. The compiler re-reads
canonical jobs and audits before output. Tracked jobs are Skip; only one Apply
per employer/role is emitted within a run.

Apply requires all of:

- Matching `evidence_sha256`; listed/published posting with apply URL.
- Explicit true `checks`: `us_location`, `base_salary`, `technical_fit`,
  `seniority`, `eligibility`, `work_arrangement`.
- `location_country: "US"`; annual USD `base_salary_min_usd` and
  `base_salary_max_usd`; `base_salary_evidence`. Maximum must exceed the canonical
  threshold. Crossing ranges need `above_threshold_rationale`.
- Relocation hybrid additionally needs `office_city`, `office_state`,
  `relocation_required: true`, and evidenced `onsite_cadence`. California,
  Massachusetts and Chicago are rejected.
- Prior-assessed postings need `recheck_reason` explaining material new evidence.

Checks are reviewer attestations, not automatic proof. Review against private
verified experience; never infer citizenship, clearance, language depth or a
submission. Missing material evidence remains Hold.

Copy reviewed rows and coverage to the canonical repo, append distinct Apply
leads/useful Holds to jobs.csv, run its validator, check latest main and commit
atomically. Never append applications absent actual submission. Preserve old
audits and V1 baseline. Keep raw responses local; record source metadata, hashes,
and decisions without unrelated content in the canonical repository.

## Limits

Listing presence does not prove a form accepts submission. Custom employer URLs
work through board retrieval; standalone verification recognizes supported ATS
patterns only. No retry loop or browser fallback. EU Lever is offline-tested;
live smoke covers US Lever. Salary/office cadence/fit remain reviewed steps.
Offline CI is deterministic; live smoke is explicitly invoked.

References: [Ashby](https://developers.ashbyhq.com/docs/public-job-posting-api),
[Greenhouse](https://docs.greenhouse.io/job-board.html),
[Lever](https://github.com/lever/postings-api).
