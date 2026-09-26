# Scoring regression fixtures

`tests/AgenticJobSearch.Tests/Fixtures/scoring-v1.json` is the replayable baseline for deterministic scoring. It contains fictional candidate profiles, job descriptions, and expected outcomes without any real candidate information.

Each case checks:

- eligibility
- an allowed fit-score range
- an allowed application-priority range
- the recommendation
- factors that must be present

Ranges preserve meaningful ranking behavior without coupling the suite to every arithmetic implementation detail. A scoring change that intentionally moves a result outside its range must update the implementation version and fixture set together, with the reason documented in the pull request.

The initial cases cover strong technical alignment, weak alignment, seniority mismatch, sponsorship ambiguity, unusually heavy on-call work, and a high-fit role above the target level. Candidate profiles already include preferences and verified evidence so the same fixture format can exercise profile-driven rules in the next milestone.
