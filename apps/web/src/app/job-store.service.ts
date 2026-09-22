import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize, Observable, tap } from 'rxjs';
import { AddJobRequest, Job } from './job.model';

@Injectable({ providedIn: 'root' })
export class JobStore {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = 'http://localhost:5156/api';

  readonly jobs = signal<Job[]>([]);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly error = signal('');

  constructor() { this.loadJobs(); }

  loadJobs(): void {
    this.isLoading.set(true);
    this.error.set('');
    this.http.get<Job[]>(`${this.apiBaseUrl}/jobs`).subscribe({
      next: (jobs) => { this.jobs.set(jobs); this.isLoading.set(false); },
      error: () => { this.error.set('Could not load applications. Make sure the API is running on port 5156.'); this.isLoading.set(false); }
    });
  }

  addJob(request: AddJobRequest): Observable<Job> {
    this.error.set('');
    this.isSaving.set(true);
    return this.http.post<Job>(`${this.apiBaseUrl}/jobs`, request).pipe(
      tap((job) => this.jobs.update((jobs) => [job, ...jobs])),
      finalize(() => this.isSaving.set(false))
    );
  }

  setError(message: string): void { this.error.set(message); }
}
