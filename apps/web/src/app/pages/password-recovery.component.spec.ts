import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Location } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { PasswordRecoveryComponent } from './password-recovery.component';
import { AuthService } from '../auth.service';
import { JobStore } from '../job-store.service';

describe('PasswordRecoveryComponent', () => {
  const token = 'A'.repeat(64);
  let snapshot: { routeConfig: { path: string }; fragment: string };
  let location: jasmine.SpyObj<Location>;
  const user = signal<unknown>({ id: 'fictional-user' });
  const jobs = signal<unknown[]>([{ id: 'fictional-job' }]);
  beforeEach(() => {
    snapshot = { routeConfig: { path: 'reset-password' }, fragment: `token=${token}` };
    location = jasmine.createSpyObj('Location', ['replaceState']);
    TestBed.configureTestingModule({ imports: [PasswordRecoveryComponent], providers: [
      provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
      { provide: ActivatedRoute, useValue: { snapshot } }, { provide: Location, useValue: location },
      { provide: AuthService, useValue: { user } }, { provide: JobStore, useValue: { jobs } }
    ] });
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());
  it('removes the fragment, posts the token once, and clears cached account data', () => {
    const component = TestBed.createComponent(PasswordRecoveryComponent).componentInstance;
    expect(location.replaceState).toHaveBeenCalledWith('/reset-password');
    expect(document.querySelector('meta[name="referrer"]')?.getAttribute('content')).toBe('no-referrer');
    component.password = component.confirmPassword = 'Fictional password 2026';
    component.submit();
    const request = TestBed.inject(HttpTestingController).expectOne('/api/account/reset-password');
    expect(request.request.body.token).toBe(token);
    request.flush({ message: 'Password reset. Sign in.' });
    expect(component.completed).toBeTrue(); expect(component.password).toBe('');
    expect(user()).toBeNull(); expect(jobs()).toEqual([]);
    component.submit();
    TestBed.inject(HttpTestingController).expectNone('/api/account/reset-password');
  });
  it('rejects a missing token without making a reset request', () => {
    snapshot.fragment = '';
    const component = TestBed.createComponent(PasswordRecoveryComponent).componentInstance;
    expect(component.missingToken).toBeTrue(); component.submit();
    TestBed.inject(HttpTestingController).expectNone('/api/account/reset-password');
  });
  it('keeps mismatches local and shows rate-limit feedback', () => {
    const component = TestBed.createComponent(PasswordRecoveryComponent).componentInstance;
    component.password = 'Fictional password 2026'; component.confirmPassword = 'Different password 2026'; component.submit();
    expect(component.error).toContain('do not match');
    TestBed.inject(HttpTestingController).expectNone('/api/account/reset-password');
    component.confirmPassword = component.password; component.submit();
    TestBed.inject(HttpTestingController).expectOne('/api/account/reset-password').flush({}, { status: 429, statusText: 'Too many requests' });
    expect(component.error).toContain('15 minutes'); expect(component.busy).toBeFalse();
  });
  it('requests a link with trimmed email and displays the generic response', () => {
    snapshot.routeConfig.path = 'forgot-password'; snapshot.fragment = '';
    const component = TestBed.createComponent(PasswordRecoveryComponent).componentInstance;
    component.email = ' fictional@example.test '; component.submit();
    const request = TestBed.inject(HttpTestingController).expectOne('/api/account/forgot-password');
    expect(request.request.body).toEqual({ email: 'fictional@example.test' });
    request.flush({ message: 'If an account exists, we will send a link.' });
    expect(component.message).toContain('If an account exists'); expect(component.completed).toBeTrue();
  });
});
