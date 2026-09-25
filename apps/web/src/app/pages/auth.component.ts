import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({ selector: 'app-auth', imports: [FormsModule, RouterLink], templateUrl: './auth.component.html', changeDetection: ChangeDetectionStrategy.Eager })
export class AuthComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly register = inject(ActivatedRoute).snapshot.routeConfig?.path === 'register';
  displayName = ''; email = ''; password = ''; confirmPassword = ''; busy = false; error = ''; showPassword = false;
  submit(): void {
    if (this.busy) return;
    if (this.register && this.password !== this.confirmPassword) { this.error = 'Your passwords do not match.'; return; }
    this.busy = true; this.error = '';
    this.auth.authenticate(this.register ? 'register' : 'login', { displayName: this.displayName.trim(), email: this.email.trim(), password: this.password }).subscribe({
      next: user => { this.password = ''; this.confirmPassword = ''; void this.router.navigateByUrl(user.careerStage ? '/' : '/onboarding'); },
      error: response => { this.busy = false; this.error = response.status === 429 ? 'Too many attempts. Please wait a minute and try again.' : response.error?.message ?? 'Unable to connect. Please try again.'; }
    });
  }
}
