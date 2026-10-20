import { test } from 'node:test';
import assert from 'node:assert/strict';
import { resumeEvidenceCandidates, reviewedResumeEvidence } from './resume-evidence-import.model.ts';

test('previews only explicit resume bullets without inventing or rewriting claims', () => {
  const resume = [
    'FICTIONAL RESUME',
    'Experience',
    '• Coordinated a fictional research team across three projects.',
    '- Built fictional reporting workflows with SQL and Python.',
    'Summary text is not promoted to evidence.',
    '1. Documented fictional customer support patterns for a team.',
    '• Short skill',
  ].join('\n');

  assert.deepEqual(resumeEvidenceCandidates(resume, []), [
    'Coordinated a fictional research team across three projects.',
    'Built fictional reporting workflows with SQL and Python.',
    'Documented fictional customer support patterns for a team.'
  ]);
});

test('excludes existing and repeated evidence statements', () => {
  const statement = 'Documented fictional customer support patterns for a team.';
  assert.deepEqual(resumeEvidenceCandidates(`• ${statement}\n- ${statement.toUpperCase()}`, [
    { category: 'Resume', statement, verificationStatus: 'NeedsReview', source: 'Pasted resume' }
  ]), []);
});

test('selected statements remain unverified until separately reviewed', () => {
  const statement = 'Built fictional reporting workflows with SQL and Python.';
  assert.deepEqual(reviewedResumeEvidence([statement]), [
    { category: 'Resume', statement, verificationStatus: 'NeedsReview', source: 'Pasted resume' }
  ]);
  assert.deepEqual(reviewedResumeEvidence([]), []);
});
