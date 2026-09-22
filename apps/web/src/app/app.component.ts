import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type JobEvaluationFactor = { name: string; weight: number; scoreImpact: number; rationale: string };
type ApplicationSummary = {
  externalId?: string; state: string; submittedDate?: string; resumeVersion?: string;
  nextFollowUp?: string; outcome?: string; notes?: string;
};
type Job = {
  id: string; externalId?: string; title: string; company: string; location: string; sourceUrl?: string;
  trackingStage?: string; salaryText?: string; priority?: string; fitRationale?: string; gapsNotes?: string;
  statusDate?: string; verifiedDate?: string; application?: ApplicationSummary; seniority: string; workMode: string;
  lifecycleState: string; eligibility: string; fitScore: number; applicationPriority: number; recommendation: string;
  explanation: string; factors: JobEvaluationFactor[]; createdAt: string;
};
type AddJobRequest = { title?: string; company?: string; location?: string; sourceUrl?: string; sourceText: string };
type StatusFilter = 'all' | 'lead' | 'applied' | 'interviewing' | 'offer' | 'rejected' | 'withdrawn' | 'closed';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.component.css'
})
export class AppComponent {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = 'http://localhost:5156/api';
  readonly jobs = signal<Job[]>([]);
  readonly selectedJob = signal<Job | null>(null);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly showAddJob = signal(false);
  readonly error = signal('');
  readonly search = signal('');
  readonly statusFilter = signal<StatusFilter>('all');

  readonly filteredJobs = computed(() => {
    const query = this.search().trim().toLocaleLowerCase();
    const filter = this.statusFilter();
    return this.jobs().filter((job) => {
      const stage = this.stage(job);
      const matchesStage = filter === 'all' || stage === filter || (filter === 'closed' && ['unavailable', 'ineligible'].includes(stage));
      const matchesQuery = !query || `${job.title} ${job.company} ${job.location}`.toLocaleLowerCase().includes(query);
      return matchesStage && matchesQuery;
    });
  });
  readonly activeCount = computed(() => this.jobs().filter((job) => ['applied', 'interviewing', 'offer'].includes(this.stage(job))).length);
  readonly interviewCount = computed(() => this.count('interviewing'));
  readonly offerCount = computed(() => this.count('offer'));
  readonly followUpCount = computed(() => this.jobs().filter((job) => this.isUpcoming(job.application?.nextFollowUp)).length);
  readonly filters: { key: StatusFilter; label: string }[] = [
    { key: 'all', label: 'All' }, { key: 'lead', label: 'Saved' }, { key: 'applied', label: 'Applied' },
    { key: 'interviewing', label: 'Interviewing' }, { key: 'offer', label: 'Offers' },
    { key: 'rejected', label: 'Rejected' }, { key: 'withdrawn', label: 'Withdrawn' }, { key: 'closed', label: 'Unavailable' }
  ];
  form: AddJobRequest = { title: '', company: '', location: '', sourceUrl: '', sourceText: '' };

  constructor() { this.loadJobs(); }

  loadJobs(): void {
    this.isLoading.set(true);
    this.error.set('');
    this.http.get<Job[]>(`${this.apiBaseUrl}/jobs`).subscribe({
      next: (jobs) => { this.jobs.set(jobs); this.selectedJob.set(this.keepSelection(jobs)); this.isLoading.set(false); },
      error: () => { this.error.set('Could not load applications. Make sure the API is running on port 5156.'); this.isLoading.set(false); }
    });
  }

  addJob(): void {
    this.error.set('');
    if (!this.form.sourceText?.trim()) { this.error.set('Paste a job description before scoring.'); return; }
    this.isSaving.set(true);
    this.http.post<Job>(`${this.apiBaseUrl}/jobs`, this.cleanRequest(this.form)).subscribe({
      next: (job) => {
        this.jobs.set([job, ...this.jobs()]); this.selectedJob.set(job);
        this.form = { title: '', company: '', location: '', sourceUrl: '', sourceText: '' };
        this.isSaving.set(false); this.showAddJob.set(false);
      },
      error: (response) => { this.error.set(response?.error?.error ?? 'The job could not be scored.'); this.isSaving.set(false); }
    });
  }

  selectJob(job: Job): void { this.selectedJob.set(job); }
  setFilter(filter: StatusFilter): void {
    this.statusFilter.set(filter);
    const selected = this.selectedJob();
    if (!selected || !this.filteredJobs().some((job) => job.id === selected.id)) this.selectedJob.set(this.filteredJobs()[0] ?? null);
  }
  stage(job: Job): string {
    return job.trackingStage?.toLocaleLowerCase() ?? (job.application ? this.applicationStage(job.application.state) : 'lead');
  }
  stageLabel(job: Job): string {
    const labels: Record<string, string> = { lead: 'Saved', applied: 'Applied', interviewing: 'Interviewing', offer: 'Offer', rejected: 'Rejected', withdrawn: 'Withdrawn', unavailable: 'Unavailable', ineligible: 'Ineligible' };
    return labels[this.stage(job)] ?? 'Saved';
  }
  statusDate(job: Job): string { return this.formatDate(job.statusDate ?? job.application?.submittedDate ?? job.createdAt); }
  formatDate(value?: string): string {
    if (!value) return 'Not set';
    const date = value.length === 10 ? new Date(`${value}T12:00:00`) : new Date(value);
    return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(date);
  }
  filterCount(filter: StatusFilter): number {
    if (filter === 'all') return this.jobs().length;
    if (filter === 'closed') return this.jobs().filter((job) => ['unavailable', 'ineligible'].includes(this.stage(job))).length;
    return this.count(filter);
  }
  private count(stage: string): number { return this.jobs().filter((job) => this.stage(job) === stage).length; }
  private applicationStage(state: string): string {
    const normalized = state.toLocaleLowerCase();
    if (normalized === 'submitted') return 'applied';
    if (normalized === 'interview' || normalized === 'screen') return 'interviewing';
    return normalized;
  }
  private isUpcoming(value?: string): boolean {
    if (!value) return false;
    const today = new Date(); today.setHours(0, 0, 0, 0);
    const difference = new Date(`${value}T00:00:00`).getTime() - today.getTime();
    return difference >= 0 && difference <= 7 * 24 * 60 * 60 * 1000;
  }
  private keepSelection(jobs: Job[]): Job | null {
    const currentId = this.selectedJob()?.id;
    return jobs.find((job) => job.id === currentId) ?? jobs[0] ?? null;
  }
  private cleanRequest(request: AddJobRequest): AddJobRequest {
    return { title: request.title?.trim() || undefined, company: request.company?.trim() || undefined, location: request.location?.trim() || undefined, sourceUrl: request.sourceUrl?.trim() || undefined, sourceText: request.sourceText.trim() };
  }
}
