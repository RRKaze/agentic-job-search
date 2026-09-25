import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize, Observable, tap } from 'rxjs';
import { CandidateProfile, CandidateProfileUpdate } from './candidate-profile.model';

@Injectable({ providedIn: 'root' })
export class CandidateProfileService {
  private readonly http = inject(HttpClient);
  readonly profile = signal<CandidateProfile | null>(null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);

  load(): Observable<CandidateProfile> {
    this.isLoading.set(true);
    return this.http.get<CandidateProfile>('/api/candidate-profile').pipe(
      tap(profile => this.profile.set(profile)),
      finalize(() => this.isLoading.set(false))
    );
  }

  save(update: CandidateProfileUpdate): Observable<CandidateProfile> {
    this.isSaving.set(true);
    return this.http.put<CandidateProfile>('/api/candidate-profile', update).pipe(
      tap(profile => this.profile.set(profile)),
      finalize(() => this.isSaving.set(false))
    );
  }
}
