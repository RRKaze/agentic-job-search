import { ChangeDetectionStrategy, Component, computed, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AddJobRequest, dateTimestamp, formatDate, isSavedOpportunity, Job, jobStage, stageLabel, statusDateValue } from '../job.model';
import { JobStore } from '../job-store.service';

@Component({
  selector: 'app-opportunities',
  imports: [FormsModule],
  templateUrl: './opportunities.component.html',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class OpportunitiesComponent {
  readonly store = inject(JobStore);
  readonly search = signal('');
  readonly selectedId = signal<string | null>(null);
  readonly showAddJob = signal(false);
  readonly opportunities = computed(() => {
    const query = this.search().trim().toLocaleLowerCase();
    return this.store.jobs().filter((job) => isSavedOpportunity(job) && (!query || `${job.title} ${job.company} ${job.location}`.toLocaleLowerCase().includes(query)))
      .sort((left, right) => dateTimestamp(statusDateValue(right)) - dateTimestamp(statusDateValue(left)));
  });
  readonly selectedJob = computed(() => this.opportunities().find((job) => job.id === this.selectedId()) ?? this.opportunities()[0] ?? null);
  form: AddJobRequest = { title: '', company: '', location: '', sourceUrl: '', sourceText: '' };

  @HostListener('document:keydown.escape')
  closeAddJob(): void { this.showAddJob.set(false); }

  selectJob(job: Job): void { this.selectedId.set(job.id); }
  stage(job: Job): string { return jobStage(job); }
  stageLabel(job: Job): string { return stageLabel(job); }
  statusDate(job: Job): string { return formatDate(statusDateValue(job)); }
  formatDate(value?: string): string { return formatDate(value); }

  addJob(): void {
    if (!this.form.sourceText.trim()) { this.store.setError('Paste a job description before scoring.'); return; }
    this.store.addJob(this.cleanRequest(this.form)).subscribe({
      next: (job) => {
        this.selectedId.set(job.id);
        this.form = { title: '', company: '', location: '', sourceUrl: '', sourceText: '' };
        this.showAddJob.set(false);
      },
      error: (response) => this.store.setError(response?.error?.error ?? 'The job could not be scored.')
    });
  }

  private cleanRequest(request: AddJobRequest): AddJobRequest {
    return { title: request.title?.trim() || undefined, company: request.company?.trim() || undefined, location: request.location?.trim() || undefined, sourceUrl: request.sourceUrl?.trim() || undefined, sourceText: request.sourceText.trim() };
  }
}
