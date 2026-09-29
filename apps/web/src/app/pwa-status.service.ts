import { Injectable, NgZone, inject, signal } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PwaStatusService {
  private readonly updates = inject(SwUpdate);
  private readonly zone = inject(NgZone);

  readonly online = signal(typeof navigator === 'undefined' || navigator.onLine);
  readonly updateAvailable = signal(false);
  readonly updateError = signal('');

  constructor() {
    if (typeof window !== 'undefined') {
      window.addEventListener('online', () => this.zone.run(() => this.online.set(true)));
      window.addEventListener('offline', () => this.zone.run(() => this.online.set(false)));
    }

    if (this.updates.isEnabled) {
      this.updates.versionUpdates
        .pipe(filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'))
        .subscribe(() => this.updateAvailable.set(true));
      this.updates.unrecoverable.subscribe(() => {
        this.updateError.set('This saved version can no longer run. Reload while online to recover it.');
      });
    }
  }

  installUpdate(): void {
    window.location.reload();
  }

  reload(): void {
    window.location.reload();
  }
}
