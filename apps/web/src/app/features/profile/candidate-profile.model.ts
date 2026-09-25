export type EvidenceVerificationStatus = 'Unverified' | 'Verified' | 'NeedsReview';

export type CandidateEvidence = {
  category: string;
  statement: string;
  verificationStatus: EvidenceVerificationStatus;
  source: string;
};

export type CandidateProfile = {
  id: string;
  displayName: string;
  headline: string;
  professionalSummary: string;
  targetLevel: string;
  targetRoleFamilies: string;
  targetIndustries: string;
  preferredLocations: string;
  workModePreference: string;
  employmentTypePreference: string;
  skills: string;
  workAuthorization: string;
  requiresSponsorship: boolean;
  linkedInUrl: string;
  portfolioUrl: string;
  resumeText: string;
  institution: string;
  degree: string;
  fieldOfStudy: string;
  graduationYear?: number;
  yearsExperience?: number;
  recentEmployer: string;
  recentJobTitle: string;
  evidence: CandidateEvidence[];
};

export type CandidateProfileUpdate = Omit<CandidateProfile, 'id' | 'displayName'>;

export function emptyCandidateProfile(): CandidateProfileUpdate {
  return {
    headline: '', professionalSummary: '', targetLevel: '', targetRoleFamilies: '', targetIndustries: '',
    preferredLocations: '', workModePreference: '', employmentTypePreference: '', skills: '', workAuthorization: '',
    requiresSponsorship: false, linkedInUrl: '', portfolioUrl: '', resumeText: '', institution: '', degree: '',
    fieldOfStudy: '', recentEmployer: '', recentJobTitle: '', evidence: []
  };
}

export function editableCandidateProfile(profile: CandidateProfile): CandidateProfileUpdate {
  const { id: _id, displayName: _displayName, ...editable } = profile;
  return { ...editable, evidence: editable.evidence.map(item => ({ ...item })) };
}

export function profileCompleteness(profile: CandidateProfileUpdate, careerStage: string): number {
  const common = [profile.headline, profile.professionalSummary, profile.targetRoleFamilies, profile.preferredLocations,
    profile.skills, profile.workAuthorization, profile.resumeText];
  const background = careerStage === 'new_graduate'
    ? [profile.institution, profile.degree, profile.graduationYear]
    : [profile.recentJobTitle, profile.recentEmployer, profile.yearsExperience];
  const completed = [...common, ...background].filter(value => value !== '' && value !== undefined && value !== null).length;
  return Math.round(completed / (common.length + background.length) * 100);
}
