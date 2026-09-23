import { AuthService } from './auth.service';
import { HttpClient } from '@angular/common/http';
import { effect, inject, Injectable, signal } from '@angular/core';
import { finalize, Observable, tap } from 'rxjs';
import { AddJobRequest, Job, JobWorkflowUpdate, WorkflowChange } from './job.model';

@Injectable({ providedIn: 'root' })
export class JobStore {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = '/api';

  readonly jobs = signal<Job[]>([]);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly error = signal('');

  private readonly auth = inject(AuthService);
  private activeAccount: string | null = null;
  constructor() {
    effect(() => {
      const account = this.auth.user();
      const id = account?.careerStage ? account.id : null;
      if (this.activeAccount === id) return;
      this.activeAccount = id; this.jobs.set([]);
      if (id) this.loadJobs();
    });
  }

  loadJobs(): void {
    const accountId = this.auth.user()?.id;
    if (!accountId) return;
    this.isLoading.set(true);
    this.error.set('');
    this.http.get<Job[]>(`${this.apiBaseUrl}/jobs`).subscribe({
      next: (jobs) => { if (this.auth.user()?.id !== accountId) return; this.jobs.set(jobs); this.isLoading.set(false); },
      error: () => { if (this.auth.user()?.id !== accountId) return; this.error.set('Could not load your workspace. Please try again.'); this.isLoading.set(false); }
    });
  }

  addJob(request: AddJobRequest): Observable<Job> {
    const accountId = this.auth.user()?.id;
    this.error.set('');
    this.isSaving.set(true);
    return this.http.post<Job>(`${this.apiBaseUrl}/jobs`, request).pipe(
      tap((job) => { if (this.auth.user()?.id === accountId) this.jobs.update((jobs) => [job, ...jobs]); }),
      finalize(() => this.isSaving.set(false))
    );
  }

  updateWorkflow(jobId: string, request: JobWorkflowUpdate): Observable<Job> {
    const accountId = this.auth.user()?.id;
    this.error.set('');
    this.isSaving.set(true);
    return this.http.put<Job>(`${this.apiBaseUrl}/jobs/${jobId}/workflow`, request).pipe(
      tap((updated) => {
        if (this.auth.user()?.id === accountId)
          this.jobs.update((jobs) => jobs.map((job) => job.id === updated.id ? updated : job));
      }),
      finalize(() => this.isSaving.set(false))
    );
  }

  workflowHistory(jobId: string): Observable<WorkflowChange[]> {
    return this.http.get<WorkflowChange[]>(`${this.apiBaseUrl}/jobs/${jobId}/workflow-history`);
  }

  setError(message: string): void { this.error.set(message); }
}
