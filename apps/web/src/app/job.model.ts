export type JobEvaluationFactor = { name: string; weight: number; scoreImpact: number; rationale: string };

export type ApplicationSummary = {
  externalId?: string;
  state: string;
  submittedDate?: string;
  resumeVersion?: string;
  nextFollowUp?: string;
  outcome?: string;
  notes?: string;
};

export type Job = {
  id: string;
  externalId?: string;
  title: string;
  company: string;
  location: string;
  sourceUrl?: string;
  trackingStage?: string;
  salaryText?: string;
  priority?: string;
  fitRationale?: string;
  gapsNotes?: string;
  statusDate?: string;
  verifiedDate?: string;
  application?: ApplicationSummary;
  seniority: string;
  workMode: string;
  lifecycleState: string;
  eligibility: string;
  fitScore: number;
  applicationPriority: number;
  recommendation: string;
  explanation: string;
  factors: JobEvaluationFactor[];
  createdAt: string;
};

export type AddJobRequest = {
  title?: string;
  company?: string;
  location?: string;
  sourceUrl?: string;
  sourceText: string;
};

export function jobStage(job: Job): string {
  if (job.trackingStage) return job.trackingStage.toLocaleLowerCase();
  if (!job.application) return 'lead';

  const state = job.application.state.toLocaleLowerCase();
  if (state === 'submitted') return 'applied';
  if (state === 'interview' || state === 'screen') return 'interviewing';
  return state;
}

export function stageLabel(job: Job): string {
  const labels: Record<string, string> = {
    lead: 'Saved', applied: 'Applied', interviewing: 'Interviewing', offer: 'Offer', rejected: 'Rejected',
    withdrawn: 'Withdrawn', unavailable: 'Unavailable', ineligible: 'Ineligible'
  };
  return labels[jobStage(job)] ?? 'Saved';
}

export function statusDateValue(job: Job): string {
  return job.statusDate ?? job.application?.submittedDate ?? job.createdAt;
}

export function dateTimestamp(value: string): number {
  const timestamp = new Date(value.length === 10 ? `${value}T12:00:00` : value).getTime();
  return Number.isNaN(timestamp) ? 0 : timestamp;
}

export function formatDate(value?: string): string {
  if (!value) return 'Not set';
  const date = new Date(value.length === 10 ? `${value}T12:00:00` : value);
  return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(date);
}
