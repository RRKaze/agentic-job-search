import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({ selector: 'app-profile', imports: [FormsModule, DatePipe], templateUrl: './profile.component.html', changeDetection: ChangeDetectionStrategy.Eager })
export class ProfileComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly onboarding = inject(ActivatedRoute).snapshot.routeConfig?.path === 'onboarding';
  displayName = this.auth.user()?.displayName ?? '';
  careerStage = this.auth.user()?.careerStage ?? '';
  busy = false; error = ''; saved = false;
  save(): void {
    if (this.busy || !this.careerStage || !this.displayName.trim()) return;
    this.busy = true; this.error = ''; this.saved = false;
    this.auth.saveProfile(this.displayName.trim(), this.careerStage).subscribe({
      next: user => { this.displayName = user.displayName; this.busy = false; this.saved = true; if (this.onboarding) void this.router.navigateByUrl('/'); },
      error: response => { this.busy = false; this.error = response.status === 401 ? 'Your session expired. Please sign out and sign in again.' : response.error?.message ?? 'We couldn’t save your profile. Please try again.'; }
    });
  }
}
