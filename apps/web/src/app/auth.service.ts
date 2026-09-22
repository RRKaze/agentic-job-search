import { inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, firstValueFrom, tap, throwError } from 'rxjs';

export interface Account { id: string; displayName: string; email: string; careerStage: 'new_graduate' | 'experienced_worker' | null; createdAt: string; }
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  readonly user = signal<Account | null>(null);
  readonly unavailable = signal(false);
  private pending?: Promise<void>;
  refresh(): Promise<void> {
    if (this.pending) return this.pending;
    this.pending = firstValueFrom(this.http.get<Account>('/api/account/me')).then(user => {
      this.user.set(user); this.unavailable.set(false);
    }).catch((error: HttpErrorResponse) => {
      this.user.set(null); this.unavailable.set(error.status !== 401);
    }).finally(() => { this.pending = undefined; });
    return this.pending;
  }
  authenticate(mode: 'register' | 'login', data: object) {
    return this.http.post<Account>(`/api/account/${mode}`, data).pipe(tap(user => { this.user.set(user); this.unavailable.set(false); }));
  }
  saveProfile(displayName: string, careerStage: string) {
    return this.http.put<Account>('/api/account/profile', { displayName, careerStage }).pipe(tap(user => this.user.set(user)));
  }
  logout() { return this.http.post<void>('/api/account/logout', {}).pipe(tap(() => this.user.set(null))); }
}
export const accountGuard: CanActivateFn = async (route) => {
  const auth = inject(AuthService); const router = inject(Router);
  await auth.refresh();
  if (!auth.user()) return router.createUrlTree(['/sign-in']);
  if (!auth.user()!.careerStage && route.routeConfig?.path !== 'onboarding') return router.createUrlTree(['/onboarding']);
  if (auth.user()!.careerStage && route.routeConfig?.path === 'onboarding') return router.createUrlTree(['/']);
  return true;
};
export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthService); const router = inject(Router);
  await auth.refresh();
  return auth.user() ? router.createUrlTree([auth.user()!.careerStage ? '/' : '/onboarding']) : true;
};
export const sessionInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService); const router = inject(Router);
  if (!request.url.startsWith('/api/')) return next(request);
  return next(request.clone({ withCredentials: true, setHeaders: { 'X-Agentic-Request': '1' } })).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !request.url.startsWith('/api/account/')) { auth.user.set(null); void router.navigateByUrl('/sign-in'); }
      return throwError(() => error);
    })
  );
};
