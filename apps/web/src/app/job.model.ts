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

export type JobWorkflowUpdate = {
  status: string;
  statusDate: string;
  submittedDate?: string;
  resumeVersion?: string;
  nextFollowUp?: string;
  outcome?: string;
  notes?: string;
};

export type WorkflowChange = {
  previousStatus: string;
  currentStatus: string;
  changedAt: string;
};

export function jobStage(job: Job): string {
  if (job.trackingStage) return job.trackingStage.toLocaleLowerCase();
  if (!job.application) return 'lead';

  const state = job.application.state.toLocaleLowerCase();
  if (state === 'submitted') return 'applied';
  if (state === 'interview' || state === 'screen') return 'interviewing';
  return state;
}

export function isSavedOpportunity(job: Job): boolean {
  return !job.application && jobStage(job) === 'lead';
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

export function todayInput(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function workflowStatusOptions(job: Job): { value: string; label: string }[] {
  const current = jobStage(job);
  const transitions: Record<string, string[]> = {
    lead: ['applied'],
    applied: ['interviewing', 'rejected', 'withdrawn'],
    interviewing: ['applied', 'offer', 'rejected', 'withdrawn'],
    offer: ['interviewing', 'rejected', 'withdrawn'],
    rejected: ['applied', 'interviewing'],
    withdrawn: ['applied']
  };
  const labels: Record<string, string> = {
    lead: 'Saved', applied: 'Applied', interviewing: 'Interviewing', offer: 'Offer', rejected: 'Rejected', withdrawn: 'Withdrawn'
  };
  return [current, ...(transitions[current] ?? [])].map((value) => ({ value, label: labels[value] ?? value }));
}
