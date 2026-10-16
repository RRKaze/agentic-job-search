import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Job } from '../job.model';
import { JobStore } from '../job-store.service';
import { ApplicationsComponent } from './applications.component';
import { OpportunitiesComponent } from './opportunities.component';

const job = (id: string, stage: string, date: string): Job => ({
  id, title: `Engineer ${id}`, company: 'Example', location: 'Remote', createdAt: `${date}T12:00:00Z`,
  statusDate: date, trackingStage: stage, seniority: 'Senior', workMode: 'Remote', lifecycleState: 'Analyzed',
  eligibility: 'Eligible', fitScore: 80, applicationPriority: 80, recommendation: 'Review', explanation: '', factors: [],
  ...(stage === 'lead' ? {} : { application: { state: stage === 'applied' ? 'Submitted' : 'Rejected' } })
});

function provideStore(jobs: Job[]) {
  return {
    provide: JobStore,
    useValue: {
      jobs: signal(jobs), isLoading: signal(false), isSaving: signal(false), error: signal(''),
      workflowHistory: () => of([]), loadJobs: () => {}, updateWorkflow: () => of(null)
    }
  };
}

describe('tracker status controls', () => {
  it('shows saved status as read-only and sorts discovered dates when the header is clicked', () => {
    TestBed.configureTestingModule({ imports: [OpportunitiesComponent], providers: [provideStore([
      { ...job('old', 'lead', '2026-09-30'), fitRationale: 'Relevant skills. Remote role.', gapsNotes: 'Travel; late hours' }, job('new', 'lead', '2026-10-15')
    ])] });
    const fixture = TestBed.createComponent(OpportunitiesComponent);
    fixture.detectChanges();
    const rows = () => [...fixture.nativeElement.querySelectorAll('.application-row')] as HTMLElement[];
    expect(rows().map((row) => row.querySelector('.job-identity strong')?.textContent)).toEqual(['Engineer new', 'Engineer old']);
    expect(rows()[0].querySelector('.status-badge')?.textContent).toContain('Saved');
    expect(rows()[0].querySelector('button, select')).toBeNull();
    expect(fixture.nativeElement.querySelector('.detail-actions')).toBeNull();

    (fixture.nativeElement.querySelector('.sort-button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(rows().map((row) => row.querySelector('.job-identity strong')?.textContent)).toEqual(['Engineer old', 'Engineer new']);
    rows()[0].click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.detail-actions button')?.textContent).toContain('Move to Applied');
    expect([...fixture.nativeElement.querySelectorAll('.detail-note li')].map((item: Element) => item.textContent?.trim())).toEqual([
      'Relevant skills.', 'Remote role.', 'Travel', 'late hours'
    ]);
    expect(fixture.nativeElement.querySelector('.detail-panel.mobile-open')).not.toBeNull();
    (fixture.nativeElement.querySelector('.mobile-detail-close') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.detail-panel.mobile-open')).toBeNull();
  });

  it('keeps rejected status fixed while allowing edits to other application details', async () => {
    TestBed.configureTestingModule({ imports: [ApplicationsComponent], providers: [provideStore([
      job('rejected', 'rejected', '2026-10-15')
    ])] });
    const fixture = TestBed.createComponent(ApplicationsComponent);
    fixture.detectChanges();
    const row = fixture.nativeElement.querySelector('.application-row') as HTMLElement;
    expect(row.querySelector('.status-badge')?.textContent).toContain('Rejected');
    expect(row.querySelector('select, button')).toBeNull();
    expect(fixture.nativeElement.querySelector('.workflow-form')).toBeNull();

    row.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const status = fixture.nativeElement.querySelector('select[name="workflowStatus"]') as HTMLSelectElement;
    expect([...status.options].map((option) => option.value)).toEqual(['rejected']);
    expect(status.disabled).toBeTrue();
    expect(status.closest('label')?.textContent).toContain('Rejected is final');
    expect((fixture.nativeElement.querySelector('textarea[name="workflowNotes"]') as HTMLTextAreaElement).disabled).toBeFalse();
    (fixture.nativeElement.querySelector('.mobile-detail-close') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.workflow-form')).toBeNull();
  });
});
