import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { dateTimestamp, formatDate, isSavedOpportunity, Job, jobStage, stageLabel, statusDateValue } from '../job.model';
import { JobStore } from '../job-store.service';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class DashboardComponent {
  readonly store = inject(JobStore);
  readonly jobs = this.store.jobs;
  readonly activeCount = computed(() => this.jobs().filter((job) => ['applied', 'interviewing', 'offer'].includes(jobStage(job))).length);
  readonly interviewCount = computed(() => this.jobs().filter((job) => jobStage(job) === 'interviewing').length);
  readonly offerCount = computed(() => this.jobs().filter((job) => jobStage(job) === 'offer').length);
  readonly followUpCount = computed(() => this.jobs().filter((job) => this.isUpcoming(job.application?.nextFollowUp)).length);
  readonly recentOpportunities = computed(() => this.byMostRecent(this.jobs().filter((job) => isSavedOpportunity(job))).slice(0, 5));
  readonly recentApplications = computed(() => this.byMostRecent(this.jobs().filter((job) => Boolean(job.application))).slice(0, 6));

  stage(job: Job): string { return jobStage(job); }
  stageLabel(job: Job): string { return stageLabel(job); }
  statusDate(job: Job): string { return formatDate(statusDateValue(job)); }

  private byMostRecent(jobs: Job[]): Job[] {
    return [...jobs].sort((left, right) => dateTimestamp(statusDateValue(right)) - dateTimestamp(statusDateValue(left)));
  }

  private isUpcoming(value?: string): boolean {
    if (!value) return false;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const difference = new Date(`${value}T00:00:00`).getTime() - today.getTime();
    return difference >= 0 && difference <= 7 * 24 * 60 * 60 * 1000;
  }
}
