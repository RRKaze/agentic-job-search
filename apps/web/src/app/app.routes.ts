import { Routes } from '@angular/router';
import { ApplicationsComponent } from './pages/applications.component';
import { DashboardComponent } from './pages/dashboard.component';
import { OpportunitiesComponent } from './pages/opportunities.component';

export const routes: Routes = [
  { path: '', component: DashboardComponent, title: 'Dashboard · Agentic Job Search' },
  { path: 'applications', component: ApplicationsComponent, title: 'Applications · Agentic Job Search' },
  { path: 'opportunities', component: OpportunitiesComponent, title: 'Opportunities · Agentic Job Search' },
  { path: '**', redirectTo: '' }
];
