type WorkflowJob = {
  trackingStage?: string;
  statusDate?: string;
  createdAt: string;
  application?: {
    state: string;
    submittedDate?: string;
    resumeVersion?: string;
    nextFollowUp?: string;
    outcome?: string;
    notes?: string;
  };
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

export function workflowStatusOptions(job: WorkflowJob): { value: string; label: string }[] {
  const current = workflowStage(job);
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

export function workflowFormFor(job: WorkflowJob): JobWorkflowUpdate {
  return {
    status: workflowStage(job),
    statusDate: (job.statusDate ?? job.application?.submittedDate ?? job.createdAt).slice(0, 10),
    submittedDate: job.application?.submittedDate,
    resumeVersion: job.application?.resumeVersion,
    nextFollowUp: job.application?.nextFollowUp,
    outcome: job.application?.outcome,
    notes: job.application?.notes
  };
}

function workflowStage(job: WorkflowJob): string {
  if (job.trackingStage) return job.trackingStage.toLocaleLowerCase();
  if (!job.application) return 'lead';
  const state = job.application.state.toLocaleLowerCase();
  return state === 'submitted' ? 'applied' : state === 'interview' || state === 'screen' ? 'interviewing' : state;
}

export function cleanWorkflowUpdate(request: JobWorkflowUpdate): JobWorkflowUpdate {
  const text = (value?: string) => value?.trim() || undefined;
  return {
    ...request,
    submittedDate: text(request.submittedDate),
    resumeVersion: text(request.resumeVersion),
    nextFollowUp: text(request.nextFollowUp),
    outcome: text(request.outcome),
    notes: text(request.notes)
  };
}
