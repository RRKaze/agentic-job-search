import { ChangeDetectionStrategy, Component, computed, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AddJobRequest, formatDate, isSavedOpportunity, Job, jobStage, sortByDiscoveredDate, stageLabel, statusDateValue, todayInput } from '../job.model';
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
  readonly discoveredDirection = signal<'asc' | 'desc'>('desc');
  readonly selectedId = signal<string | null>(null);
  readonly showAddJob = signal(false);
  readonly opportunities = computed(() => {
    const query = this.search().trim().toLocaleLowerCase();
    return sortByDiscoveredDate(
      this.store.jobs().filter((job) => isSavedOpportunity(job) && (!query || `${job.title} ${job.company} ${job.location}`.toLocaleLowerCase().includes(query))),
      this.discoveredDirection()
    );
  });
  readonly selectedJob = computed(() => this.opportunities().find((job) => job.id === this.selectedId()) ?? null);
  form: AddJobRequest = { title: '', company: '', location: '', sourceUrl: '', sourceText: '' };

  @HostListener('document:keydown.escape')
  closeAddJob(): void { this.showAddJob.set(false); }

  selectJob(job: Job): void { this.selectedId.set(job.id); }
  toggleDiscoveredSort(): void { this.discoveredDirection.update((direction) => direction === 'asc' ? 'desc' : 'asc'); }
  markApplied(job: Job): void {
    const today = todayInput();
    this.store.updateWorkflow(job.id, { status: 'applied', statusDate: today, submittedDate: today }).subscribe({
      next: () => this.selectedId.set(null),
      error: (response) => this.store.setError(response?.error?.message ?? 'The opportunity could not be moved to Applications.')
    });
  }
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
