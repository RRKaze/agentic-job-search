# Public ATS verification adapter (initial implementation)

The read-only verifier is [scripts/verify_ats.py](../scripts/verify_ats.py). It supports:
- Ashby: public `posting-api/job-board/{board}?includeCompensation=true` (salary summary when supplied)
- Greenhouse: public `v1/boards/{board}/jobs?content=true`
- Lever: public `v0/postings/{board}?mode=json`

Run `python3 scripts/verify_ats.py 'https://jobs.ashbyhq.com/BOARD/POSTING_ID' --json`. Other supported URL formats: `https://job-boards.greenhouse.io/BOARD/jobs/ID` and `https://jobs.lever.co/BOARD/ID`.

Outcomes:
- `published`: matching job ID appears on the currently published public board. **Not** confirmation the apply form will succeed, nor verification of US eligibility or salary minimum.
- `not_found`: API returned a valid listing but ID was absent. Could be unpublished, unlisted, moved, or closed; do not automatically classify as closed.
- `unverified`: unsupported URL, HTTP error, timeout or unexpected response; retain Hold and investigate.
- No job application is submitted. No user data is sent.

The caller must perform separate salary, citizenship/region, seniority, technical-fit, work-arrangement and cross-run deduplication checks. Missing compensation is **Hold**, not an assumed qualifying posting. The script does not write to either real tracker or audit.

Validation: `python3 -m unittest discover -s tests -p 'test_verify_ats.py'` tests URL recognition and response interpretation with mocked data (no live HTTP needed).

Known limitations / planned next increments:
1. Add support for additional employer ATS URLs, including some `gh_jid` embedded employer career pages.
2. Build a restrictive, read-only Playwright fallback only for ATS API inconclusive results. Do not bypass CAPTCHA, logins, or security controls; block unsafe redirects, prohibit application submission and cap timeouts.
3. Integrate verified result metadata into the Version 2 search-coverage/audit workflow in `RRKaze/job-search` without storing real applications or applicant identity in this engineering repository.
4. Validate against a sample of live boards and the unit test suite before claiming production readiness. Currently unit tests are committed but no CI result or live verification has been observed.

API references: https://developers.ashbyhq.com/docs/public-job-posting-api ; https://docs.greenhouse.io/job-board.html ; https://github.com/lever/postings-api
