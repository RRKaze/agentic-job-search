import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { formatDate, Job, jobStage, stageLabel, statusDateValue, dateTimestamp } from '../job.model';
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

  selectJob(job: Job): void { this.selectedId.set(job.id); }
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
}
