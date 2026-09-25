# Candidate Profile V1

Candidate Profile V1 is occupation-neutral. It records the information needed to compare a person with an opportunity without assuming that the person works in software engineering.

## Profile sections

- Professional identity: headline and summary.
- Career goals: target roles, level, industries, locations, work arrangement, and employment type.
- Career background: education for new graduates or recent work context for experienced workers.
- Qualifications: skills, methods, specialties, work authorization, and sponsorship needs.
- Documents: LinkedIn, portfolio or professional website, and pasted resume text.
- Evidence: sourced accomplishment statements with an explicit verification status.

The career-stage choice adapts which background questions are emphasized. It does not restrict which professions or roles a person can pursue. All role, industry, skill, and evidence fields accept occupation-neutral language.

## Evidence safety

Evidence can be `Unverified`, `NeedsReview`, or `Verified`. A source should identify where the claim can be checked, such as a resume, transcript, portfolio, certification, performance review, or manager feedback. Future resume tailoring and confirmed candidate claims must use verified evidence only.

The profile page never creates accomplishments from job descriptions or inferred experience. Users explicitly enter and classify their own evidence.

## Ownership and API

`GET /api/candidate-profile` and `PUT /api/candidate-profile` operate only on the signed-in account's profile. Mutations require the same custom request header used by other browser writes. The backend validates lengths, allowed preference values, year and experience ranges, URL schemes, evidence count, and required evidence content.

The profile and its evidence are persisted in PostgreSQL. Resume text is stored as account-owned profile data. File upload is intentionally deferred until private document storage, content validation, and retention rules are designed.

## Next integration

The next milestone should feed profile goals, preferences, skills, and verified evidence into deterministic opportunity scoring. Existing scoring behavior remains unchanged until that work is implemented and tested separately.
