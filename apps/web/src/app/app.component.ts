import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type JobEvaluationFactor = {
  name: string;
  weight: number;
  scoreImpact: number;
  rationale: string;
};

type Job = {
  id: string;
  title: string;
  company: string;
  location: string;
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

type AddJobRequest = {
  title?: string;
  company?: string;
  location?: string;
  sourceUrl?: string;
  sourceText: string;
};

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = 'http://localhost:5156/api';

  readonly jobs = signal<Job[]>([]);
  readonly selectedJob = signal<Job | null>(null);
  readonly isSaving = signal(false);
  readonly error = signal('');

  form: AddJobRequest = {
    title: '',
    company: '',
    location: '',
    sourceUrl: '',
    sourceText: ''
  };

  constructor() {
    this.loadJobs();
  }

  loadJobs(): void {
    this.http.get<Job[]>(`${this.apiBaseUrl}/jobs`).subscribe({
      next: (jobs) => {
        this.jobs.set(jobs);
        this.selectedJob.set(jobs[0] ?? null);
      },
      error: () => this.error.set('Could not load saved jobs. Make sure the API is running on port 5156.')
    });
  }

  addJob(): void {
    this.error.set('');

    if (!this.form.sourceText?.trim()) {
      this.error.set('Paste a job description before scoring.');
      return;
    }

    this.isSaving.set(true);
    this.http.post<Job>(`${this.apiBaseUrl}/jobs`, this.cleanRequest(this.form)).subscribe({
      next: (job) => {
        this.jobs.set([job, ...this.jobs()]);
        this.selectedJob.set(job);
        this.form = { title: '', company: '', location: '', sourceUrl: '', sourceText: '' };
        this.isSaving.set(false);
      },
      error: (response) => {
        this.error.set(response?.error?.error ?? 'The job could not be scored.');
        this.isSaving.set(false);
      }
    });
  }

  selectJob(job: Job): void {
    this.selectedJob.set(job);
  }

  private cleanRequest(request: AddJobRequest): AddJobRequest {
    return {
      title: request.title?.trim() || undefined,
      company: request.company?.trim() || undefined,
      location: request.location?.trim() || undefined,
      sourceUrl: request.sourceUrl?.trim() || undefined,
      sourceText: request.sourceText.trim()
    };
  }
}
