# Scoring regression fixtures

Files under `tests/AgenticJobSearch.Tests/Fixtures/` preserve replayable baselines for deterministic scoring. They contain fictional candidate profiles, job descriptions, and expected outcomes without any real candidate information. `scoring-v1.json` records the original fixed-stack behavior; `scoring-v2.json` is the active profile-driven suite.

Each case checks:

- eligibility
- an allowed fit-score range
- an allowed application-priority range
- the recommendation
- factors that must be present

Ranges preserve meaningful ranking behavior without coupling the suite to every arithmetic implementation detail. A scoring change that intentionally moves a result outside its range must update the implementation version and fixture set together, with the reason documented in the pull request.

The active cases cover role and skill alignment, symmetric seniority mismatches, work mode and location preferences, employment type, sponsorship and authorization restrictions, unusually heavy on-call work, and a high-fit role above the target level.
