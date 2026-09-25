import { ChangeDetectionStrategy, Component, ViewEncapsulation, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from './auth.service';
import { JobStore } from './job-store.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.Eager
})
export class AppComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly jobs = inject(JobStore);
  signingOut = false; signOutError = '';
  signOut(): void {
    this.signingOut = true; this.signOutError = '';
    this.auth.logout().subscribe({ next: () => { this.jobs.jobs.set([]); this.signingOut = false; void this.router.navigateByUrl('/sign-in'); }, error: () => { this.signingOut = false; this.signOutError = 'Could not sign out. Please try again.'; } });
  }
}
