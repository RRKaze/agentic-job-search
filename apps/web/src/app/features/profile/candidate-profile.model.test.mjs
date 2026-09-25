import { test } from 'node:test';
import assert from 'node:assert/strict';
import { editableCandidateProfile, emptyCandidateProfile, profileCompleteness } from './candidate-profile.model.ts';

test('profile completeness uses education for new graduates', () => {
  const profile = emptyCandidateProfile();
  profile.headline = 'Public health graduate';
  profile.professionalSummary = 'Fictional summary';
  profile.targetRoleFamilies = 'Program Coordinator';
  profile.preferredLocations = 'New York';
  profile.skills = 'Research';
  profile.workAuthorization = 'Authorized';
  profile.resumeText = 'Resume';
  profile.institution = 'Example University';
  profile.degree = 'Bachelor of Arts';
  profile.graduationYear = 2026;

  assert.equal(profileCompleteness(profile, 'new_graduate'), 100);
  assert.equal(profileCompleteness(profile, 'experienced_worker'), 70);
});

test('editable profile creates a separate evidence collection', () => {
  const profile = {
    id: 'profile', displayName: 'Example Person', ...emptyCandidateProfile(),
    evidence: [{ category: 'Research', statement: 'Completed a study.', verificationStatus: 'Verified', source: 'Portfolio' }]
  };

  const editable = editableCandidateProfile(profile);
  editable.evidence[0].statement = 'Changed locally';

  assert.equal(profile.evidence[0].statement, 'Completed a study.');
});
