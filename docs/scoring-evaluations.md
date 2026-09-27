# Versioned job evaluations

Job scoring is stored as an append-only history. Creating a job writes its first evaluation, and `POST /api/jobs/{jobId}/evaluations` evaluates the saved source text again with the current candidate profile. `GET /api/jobs/{jobId}/evaluations` returns newest-first history for the signed-in owner.

Each evaluation records:

- the scoring implementation version
- eligibility, fit score, application priority, recommendation, and explanation
- a JSON snapshot of the candidate fields available to deterministic scoring
- scoring factors and immutable copies of any verified evidence that supported them
- the evaluation timestamp

The job list presents the newest evaluation while retaining earlier results for comparison and replay. Evaluation access uses the same owner boundary as jobs; an account cannot create or read another account's evaluations.

Evidence snapshots deliberately copy the category, statement, and source instead of using a live foreign key. Editing or deleting candidate evidence therefore cannot rewrite the explanation for a historical score. Unverified and needs-review evidence cannot be attached as factor support.

The `deterministic-v1` version identifies the current hard-coded scoring policy. The next scoring milestone will make that policy profile-driven and cover it with a versioned fixture set; changing scoring behavior must increment the version.
