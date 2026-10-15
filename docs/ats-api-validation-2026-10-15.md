# API-only validation, 2026-10-15

Starting commit: `cbd99559d41d4ecda86aeaa7e0ec4f82a487352e`.
Existing 5 tests passed. Seven targeted regression tests reproduced 4 failures
and 2 errors: API error envelope became false absence; malformed row crashed;
another Ashby board URL matched; unsafe input URLs were accepted; EU Lever
mapping was absent; Lever salary was discarded. URL-only Ashby matching already
passed and now has stronger board-bound identity and internal-ID coverage.

Final offline suite: 23 passing tests in 0.007 seconds on local Python 3.14.
CI uses Python 3.12. Coverage includes tri-state safety, URL mapping, escaped
content, salary/location gates, review-evidence binding, dedup and board caching.

Live `scripts/smoke_ats.py`, October 15 at 17:41 UTC:

| Provider / board | Postings | Seconds | Result |
| --- | ---: | ---: | --- |
| Ashby / ashby | 62 | 0.463 | Pass |
| Greenhouse / launchdarkly | 53 | 0.468 | Pass |
| Lever / standtogether | 112 | 2.459 | Pass |

Every returned posting had ID, title, job URL and apply URL. Ashby/Lever URL
identities matched normalized provider/board/ID. This verifies listing retrieval,
not form acceptance or submission. An initial mixed-case Ashby board request
returned 403 while lowercase succeeded. Failures remain unverified. EU Lever has
no live test in this run.

See `ats-verification.md` for the reproducible V2 CLI workflow. Real candidate
findings belong exclusively in `RRKaze/job-search`; the engineering repository
contains no real applications or applicant identity. No Playwright used.
