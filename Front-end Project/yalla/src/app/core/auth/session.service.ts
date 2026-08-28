import { Injectable, computed, signal } from '@angular/core';
import { Session } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly current = signal<Session | null>(null);
  readonly session = this.current.asReadonly();
  readonly isAuthenticated = computed(() => this.current() !== null);
  readonly user = computed(() => this.current()?.user ?? null);
  establish(session: Session): void { this.current.set(session); }
  clear(): void { this.current.set(null); }
  sessionId(): string | undefined { return this.current()?.id; }
}
