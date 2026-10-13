import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnDestroy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DOCUMENT, Location } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';
import { JobStore } from '../job-store.service';

@Component({
  selector: 'app-password-recovery', imports: [FormsModule, RouterLink],
  templateUrl: './password-recovery.component.html', changeDetection: ChangeDetectionStrategy.Eager
})
export class PasswordRecoveryComponent implements OnDestroy {
  private readonly destroyRef = inject(DestroyRef);
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly jobs = inject(JobStore);
  private readonly route = inject(ActivatedRoute);
  readonly reset = this.route.snapshot.routeConfig?.path === 'reset-password';
  private token = '';
  email = ''; password = ''; confirmPassword = ''; busy = false; error = ''; message = ''; completed = false;
  missingToken = false;

  constructor() {
    if (this.reset) {
      this.token = new URLSearchParams(this.route.snapshot.fragment ?? '').get('token') ?? '';
      this.missingToken = !/^[0-9a-fA-F]{64}$/.test(this.token);
      // Keep the credential only in component memory; remove it from the current history entry.
      inject(Location).replaceState('/reset-password');
    }
    // Also covers navigation from a cached PWA shell where response headers may not be reapplied.
    const document = inject(DOCUMENT);
    let meta = document.querySelector<HTMLMetaElement>('meta[name="referrer"]');
    if (!meta) { meta = document.createElement('meta'); meta.name = 'referrer'; document.head.appendChild(meta); }
    meta.content = 'no-referrer';
  }

  submit(): void {
    if (this.busy || this.completed || (this.reset && this.missingToken)) return;
    if (this.reset && this.password !== this.confirmPassword) { this.error = 'Your passwords do not match.'; return; }
    this.busy = true; this.error = '';
    const body = this.reset ? { token: this.token, password: this.password, confirmPassword: this.confirmPassword } : { email: this.email.trim() };
    this.http.post<{ message: string }>(`/api/account/${this.reset ? 'reset-password' : 'forgot-password'}`, body).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: response => {
        this.busy = false; this.completed = true; this.message = response.message;
        this.password = ''; this.confirmPassword = ''; this.token = '';
        if (this.reset) { this.auth.user.set(null); this.jobs.jobs.set([]); }
      },
      error: response => {
        this.busy = false;
        this.error = response.status === 429 ? 'Too many attempts. Please wait 15 minutes before trying again.'
          : response.error?.message ?? 'Unable to connect. Please try again.';
      }
    });
  }
  ngOnDestroy(): void { this.token = ''; this.password = ''; this.confirmPassword = ''; }
}
