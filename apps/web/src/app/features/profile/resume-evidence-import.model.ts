import type { CandidateEvidence } from './candidate-profile.model';

const bullet = /^\s*(?:[-*•●▪–]|\d+[.)])\s+(.+)$/u;
const normalized = (value: string) => value.replace(/\s+/g, ' ').trim();
const identity = (value: string) => normalized(value).toLocaleLowerCase();

export function resumeEvidenceCandidates(resumeText: string, existing: CandidateEvidence[]): string[] {
  const seen = new Set(existing.map((item) => identity(item.statement)));
  const candidates: string[] = [];

  for (const line of resumeText.split(/\r?\n/)) {
    const match = line.match(bullet);
    if (!match) continue;
    const statement = normalized(match[1]);
    const key = identity(statement);
    if (statement.length < 20 || statement.length > 1200 || seen.has(key)) continue;
    seen.add(key);
    candidates.push(statement);
  }

  return candidates;
}

export function reviewedResumeEvidence(statements: string[]): CandidateEvidence[] {
  return statements.map((statement) => ({
    category: 'Resume', statement, verificationStatus: 'NeedsReview', source: 'Pasted resume'
  }));
}
