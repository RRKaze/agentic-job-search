import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { map, switchMap } from 'rxjs';
import { AuthService } from '../auth.service';
import { CandidateProfileUpdate, editableCandidateProfile, emptyCandidateProfile, profileCompleteness } from '../features/profile/candidate-profile.model';
import { CandidateProfileService } from '../features/profile/candidate-profile.service';

@Component({ selector: 'app-profile', imports: [FormsModule, DatePipe], templateUrl: './profile.component.html', changeDetection: ChangeDetectionStrategy.Eager })
export class ProfileComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly candidate = inject(CandidateProfileService);
  private readonly router = inject(Router);
  readonly onboarding = inject(ActivatedRoute).snapshot.routeConfig?.path === 'onboarding';
  readonly maxGraduationYear = new Date().getFullYear() + 10;
  displayName = this.auth.user()?.displayName ?? '';
  careerStage = this.auth.user()?.careerStage ?? '';
  draft: CandidateProfileUpdate = emptyCandidateProfile();
  busy = false;
  error = '';
  saved = false;

  ngOnInit(): void {
    if (this.onboarding) return;
    this.candidate.load().subscribe({
      next: profile => this.draft = editableCandidateProfile(profile),
      error: response => this.error = response.status === 401 ? 'Your session expired. Please sign in again.' : 'We couldn’t load your career profile.'
    });
  }

  get completion(): number { return profileCompleteness(this.draft, this.careerStage); }

  addEvidence(): void {
    this.draft.evidence.push({ category: '', statement: '', verificationStatus: 'Unverified', source: '' });
    this.saved = false;
  }

  removeEvidence(index: number): void {
    this.draft.evidence.splice(index, 1);
    this.saved = false;
  }

  save(): void {
    if (this.busy || !this.careerStage || !this.displayName.trim()) return;
    this.busy = true; this.error = ''; this.saved = false;
    const accountUpdate = this.auth.saveProfile(this.displayName.trim(), this.careerStage);
    const update = this.onboarding
      ? accountUpdate.pipe(map(() => undefined))
      : accountUpdate.pipe(switchMap(() => this.candidate.save(this.cleanDraft())), map(() => undefined));
    update.subscribe({
      next: () => {
        this.displayName = this.auth.user()?.displayName ?? this.displayName;
        this.busy = false; this.saved = true;
        if (this.onboarding) void this.router.navigateByUrl('/');
      },
      error: (response: HttpErrorResponse) => {
        this.busy = false;
        this.error = response.status === 401 ? 'Your session expired. Please sign out and sign in again.' : response.error?.message ?? 'We couldn’t save your profile. Please try again.';
      }
    });
  }

  private cleanDraft(): CandidateProfileUpdate {
    const clean = (value: string) => value.trim();
    return {
      ...this.draft,
      headline: clean(this.draft.headline), professionalSummary: clean(this.draft.professionalSummary),
      targetLevel: clean(this.draft.targetLevel), targetRoleFamilies: clean(this.draft.targetRoleFamilies),
      targetIndustries: clean(this.draft.targetIndustries), preferredLocations: clean(this.draft.preferredLocations),
      skills: clean(this.draft.skills), workAuthorization: clean(this.draft.workAuthorization),
      linkedInUrl: clean(this.draft.linkedInUrl), portfolioUrl: clean(this.draft.portfolioUrl),
      resumeText: clean(this.draft.resumeText), institution: clean(this.draft.institution), degree: clean(this.draft.degree),
      fieldOfStudy: clean(this.draft.fieldOfStudy), recentEmployer: clean(this.draft.recentEmployer),
      recentJobTitle: clean(this.draft.recentJobTitle),
      evidence: this.draft.evidence.map(item => ({ ...item, category: clean(item.category), statement: clean(item.statement), source: clean(item.source) }))
    };
  }
}
