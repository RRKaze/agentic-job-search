import { AuthComponent } from './pages/auth.component';
import { ProfileComponent } from './pages/profile.component';
import { accountGuard, guestGuard } from './auth.service';
import { Routes } from '@angular/router';
import { ApplicationsComponent } from './pages/applications.component';
import { DashboardComponent } from './pages/dashboard.component';
import { OpportunitiesComponent } from './pages/opportunities.component';

export const routes: Routes = [
  { path: 'register', component: AuthComponent, canActivate: [guestGuard], title: 'Create account · Agentic Job Search' },
  { path: 'sign-in', component: AuthComponent, canActivate: [guestGuard], title: 'Sign in · Agentic Job Search' },
  { path: 'onboarding', component: ProfileComponent, canActivate: [accountGuard], title: 'Welcome · Agentic Job Search' },
  { path: 'profile', component: ProfileComponent, canActivate: [accountGuard], title: 'Personal profile · Agentic Job Search' },
  { path: '', component: DashboardComponent, canActivate: [accountGuard], title: 'Dashboard · Agentic Job Search' },
  { path: 'applications', component: ApplicationsComponent, canActivate: [accountGuard], title: 'Applications · Agentic Job Search' },
  { path: 'opportunities', component: OpportunitiesComponent, canActivate: [accountGuard], title: 'Opportunities · Agentic Job Search' },
  { path: '**', redirectTo: '' }
];
