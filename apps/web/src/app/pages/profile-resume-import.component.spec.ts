import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../auth.service';
import { CandidateProfileService } from '../features/profile/candidate-profile.service';
import { emptyCandidateProfile } from '../features/profile/candidate-profile.model';
import { ProfileComponent } from './profile.component';

describe('reviewed resume evidence import', () => {
  it('previews without saving and adds only selected claims as Needs review', async () => {
    const statement = 'Coordinated a fictional research team across three projects.';
    const profile = {
      id: 'fictional-profile', displayName: 'Fictional Person', ...emptyCandidateProfile(),
      resumeText: `Experience\n• ${statement}\n• Built fictional reporting workflows with SQL and Python.`
    };
    const auth = {
      user: signal({ displayName: 'Fictional Person', careerStage: 'experienced_worker', email: 'fictional@example.test', createdAt: '2026-01-01' }),
      saveProfile: jasmine.createSpy('saveProfile').and.returnValue(of(null))
    };
    const candidate = {
      isLoading: signal(false), load: jasmine.createSpy('load').and.returnValue(of(profile)),
      save: jasmine.createSpy('save').and.returnValue(of(profile))
    };
    TestBed.configureTestingModule({ imports: [ProfileComponent], providers: [
      provideRouter([]), { provide: ActivatedRoute, useValue: { snapshot: { routeConfig: { path: 'profile' } } } },
      { provide: AuthService, useValue: auth }, { provide: CandidateProfileService, useValue: candidate }
    ] });

    const fixture = TestBed.createComponent(ProfileComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.resume-import-heading button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.componentInstance.draft.evidence).toEqual([]);
    expect(candidate.save).not.toHaveBeenCalled();
    const choices = fixture.nativeElement.querySelectorAll('.resume-candidate input') as NodeListOf<HTMLInputElement>;
    expect(choices.length).toBe(2);
    choices[0].click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.componentInstance.selectedResumeCount).toBe(1);
    expect((fixture.nativeElement.querySelector('.resume-import-actions button') as HTMLButtonElement).disabled).toBeFalse();
    (fixture.nativeElement.querySelector('.resume-import-actions button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.componentInstance.draft.evidence).toEqual([
      { category: 'Resume', statement, verificationStatus: 'NeedsReview', source: 'Pasted resume' }
    ]);
    expect(candidate.save).not.toHaveBeenCalled();

    fixture.componentInstance.save();
    expect(candidate.save).toHaveBeenCalledWith(jasmine.objectContaining({ evidence: [
      { category: 'Resume', statement, verificationStatus: 'NeedsReview', source: 'Pasted resume' }
    ] }));
  });
});
