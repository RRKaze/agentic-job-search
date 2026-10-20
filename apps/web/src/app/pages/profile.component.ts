import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { map, switchMap } from 'rxjs';
import { AuthService } from '../auth.service';
import { CandidateProfileUpdate, editableCandidateProfile, emptyCandidateProfile, profileCompleteness } from '../features/profile/candidate-profile.model';
import { CandidateProfileService } from '../features/profile/candidate-profile.service';
import { resumeEvidenceCandidates, reviewedResumeEvidence } from '../features/profile/resume-evidence-import.model';

type ResumeCandidate = { statement: string; selected: boolean };

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
  resumePreview: ResumeCandidate[] = [];
  resumeImportMessage = '';
  resumeImportError = '';

  ngOnInit(): void {
    if (this.onboarding) return;
    this.candidate.load().subscribe({
      next: profile => this.draft = editableCandidateProfile(profile),
      error: response => this.error = response.status === 401 ? 'Your session expired. Please sign in again.' : 'We couldn’t load your career profile.'
    });
  }

  get completion(): number { return profileCompleteness(this.draft, this.careerStage); }
  get selectedResumeCount(): number { return this.resumePreview.filter((item) => item.selected).length; }
  get availableEvidenceSlots(): number { return 25 - this.draft.evidence.length; }

  clearResumePreview(): void {
    this.resumePreview = [];
    this.resumeImportMessage = '';
    this.resumeImportError = '';
  }

  previewResumeEvidence(): void {
    this.resumePreview = resumeEvidenceCandidates(this.draft.resumeText, this.draft.evidence)
      .map((statement) => ({ statement, selected: false }));
    this.resumeImportError = '';
    this.resumeImportMessage = this.resumePreview.length
      ? 'Choose the statements you want to add. Nothing is added or saved automatically.'
      : 'No new bullet statements were found. Use bullet lines in your resume text or add evidence manually.';
  }

  addSelectedResumeEvidence(): void {
    const selected = this.resumePreview.filter((item) => item.selected);
    if (selected.length === 0) return;
    if (selected.length > this.availableEvidenceSlots) {
      this.resumeImportError = `Choose at most ${this.availableEvidenceSlots} more statements.`;
      return;
    }
    const stillAvailable = new Set(resumeEvidenceCandidates(this.draft.resumeText, this.draft.evidence));
    if (selected.some((item) => !stillAvailable.has(item.statement))) {
      this.resumeImportError = 'Your evidence changed. Preview the resume again before adding statements.';
      return;
    }
    this.draft.evidence.push(...reviewedResumeEvidence(selected.map((item) => item.statement)));
    this.resumePreview = this.resumePreview.filter((item) => !item.selected);
    this.resumeImportError = '';
    this.resumeImportMessage = `Added ${selected.length} statement${selected.length === 1 ? '' : 's'} to your draft as Needs review. Review the details below, then save your profile.`;
    this.saved = false;
  }

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
