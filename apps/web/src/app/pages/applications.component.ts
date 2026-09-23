import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { formatDate, Job, JobWorkflowUpdate, jobStage, stageLabel, statusDateValue, dateTimestamp, todayInput, WorkflowChange, workflowStatusOptions } from '../job.model';
import { JobStore } from '../job-store.service';

type StatusFilter = 'all' | 'applied' | 'interviewing' | 'offer' | 'rejected' | 'withdrawn';
type SortColumn = 'opportunity' | 'status' | 'updated';
type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-applications',
  imports: [FormsModule],
  templateUrl: './applications.component.html',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class ApplicationsComponent {
  readonly store = inject(JobStore);
  readonly search = signal('');
  readonly statusFilter = signal<StatusFilter>('all');
  readonly sortColumn = signal<SortColumn>('updated');
  readonly sortDirection = signal<SortDirection>('desc');
  readonly selectedId = signal<string | null>(null);
  readonly saveMessage = signal('');
  readonly history = signal<WorkflowChange[]>([]);
  workflowForm: JobWorkflowUpdate = { status: 'applied', statusDate: todayInput() };
  private historyJobId: string | null = null;
  private readonly collator = new Intl.Collator(undefined, { numeric: true, sensitivity: 'base' });

  readonly applicationJobs = computed(() => this.store.jobs().filter((job) => Boolean(job.application)));
  readonly filteredJobs = computed(() => {
    const query = this.search().trim().toLocaleLowerCase();
    const filter = this.statusFilter();
    return this.applicationJobs().filter((job) => {
      const matchesStage = filter === 'all' || jobStage(job) === filter;
      const matchesQuery = !query || `${job.title} ${job.company} ${job.location}`.toLocaleLowerCase().includes(query);
      return matchesStage && matchesQuery;
    }).sort((left, right) => this.compareJobs(left, right));
  });
  readonly selectedJob = computed(() => this.filteredJobs().find((job) => job.id === this.selectedId()) ?? this.filteredJobs()[0] ?? null);
  readonly filters: { key: StatusFilter; label: string }[] = [
    { key: 'all', label: 'All' }, { key: 'applied', label: 'Applied' }, { key: 'interviewing', label: 'Interviewing' },
    { key: 'offer', label: 'Offers' }, { key: 'rejected', label: 'Rejected' }, { key: 'withdrawn', label: 'Withdrawn' }
  ];

  constructor() {
    effect(() => {
      const job = this.selectedJob();
      if (job) {
        this.workflowForm = this.formFor(job);
        if (this.historyJobId !== job.id) {
          this.saveMessage.set('');
          this.loadHistory(job.id);
        }
      }
    });
  }

  selectJob(job: Job): void { this.selectedId.set(job.id); this.saveMessage.set(''); }
  statusOptions(job: Job): { value: string; label: string }[] { return workflowStatusOptions(job); }
  historyLabel(status: string): string { return status === 'lead' ? 'Saved' : status.charAt(0).toUpperCase() + status.slice(1); }
  historyDate(value: string): string { return new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)); }
  quickStatus(job: Job, status: string): void {
    if (status === jobStage(job)) return;
    this.selectJob(job);
    const request = this.formFor(job);
    request.status = status;
    request.statusDate = todayInput();
    if (status === 'rejected' || status === 'withdrawn') request.nextFollowUp = undefined;
    this.save(job, request);
  }
  setStatus(status: string): void {
    this.workflowForm.status = status;
    if (status === 'rejected' || status === 'withdrawn') this.workflowForm.nextFollowUp = undefined;
  }
  saveSelected(job: Job): void { this.save(job, { ...this.workflowForm }); }
  stage(job: Job): string { return jobStage(job); }
  stageLabel(job: Job): string { return stageLabel(job); }
  formatDate(value?: string): string { return formatDate(value); }
  statusDate(job: Job): string { return formatDate(statusDateValue(job)); }
  filterCount(filter: StatusFilter): number { return filter === 'all' ? this.applicationJobs().length : this.applicationJobs().filter((job) => jobStage(job) === filter).length; }
  setSort(column: SortColumn): void {
    if (this.sortColumn() === column) this.sortDirection.update((direction) => direction === 'asc' ? 'desc' : 'asc');
    else { this.sortColumn.set(column); this.sortDirection.set(column === 'updated' ? 'desc' : 'asc'); }
  }
  sortIndicator(column: SortColumn): string { return this.sortColumn() !== column ? '↕' : this.sortDirection() === 'asc' ? '↑' : '↓'; }

  private compareJobs(left: Job, right: Job): number {
    let comparison = 0;
    if (this.sortColumn() === 'opportunity') comparison = this.collator.compare(`${left.title} ${left.company}`, `${right.title} ${right.company}`);
    else if (this.sortColumn() === 'status') comparison = this.statusRank(left) - this.statusRank(right);
    else comparison = dateTimestamp(statusDateValue(left)) - dateTimestamp(statusDateValue(right));
    if (comparison === 0) comparison = this.collator.compare(`${left.title} ${left.company}`, `${right.title} ${right.company}`);
    return this.sortDirection() === 'asc' ? comparison : -comparison;
  }

  private statusRank(job: Job): number {
    const order = ['applied', 'interviewing', 'offer', 'rejected', 'withdrawn'];
    const index = order.indexOf(jobStage(job));
    return index === -1 ? order.length : index;
  }

  private save(job: Job, request: JobWorkflowUpdate): void {
    this.saveMessage.set('');
    this.store.updateWorkflow(job.id, this.clean(request)).subscribe({
      next: () => { this.saveMessage.set('Application updated.'); this.loadHistory(job.id, true); },
      error: (response) => this.store.setError(response?.error?.message ?? 'The application could not be updated.')
    });
  }

  private formFor(job: Job): JobWorkflowUpdate {
    return {
      status: jobStage(job),
      statusDate: statusDateValue(job).slice(0, 10),
      submittedDate: job.application?.submittedDate,
      resumeVersion: job.application?.resumeVersion,
      nextFollowUp: job.application?.nextFollowUp,
      outcome: job.application?.outcome,
      notes: job.application?.notes
    };
  }

  private clean(request: JobWorkflowUpdate): JobWorkflowUpdate {
    const text = (value?: string) => value?.trim() || undefined;
    return { ...request, submittedDate: text(request.submittedDate), resumeVersion: text(request.resumeVersion), nextFollowUp: text(request.nextFollowUp), outcome: text(request.outcome), notes: text(request.notes) };
  }

  private loadHistory(jobId: string, force = false): void {
    if (!force && this.historyJobId === jobId) return;
    this.historyJobId = jobId;
    this.store.workflowHistory(jobId).subscribe({
      next: (history) => { if (this.historyJobId === jobId) this.history.set(history); },
      error: () => { if (this.historyJobId === jobId) this.history.set([]); }
    });
  }
}
