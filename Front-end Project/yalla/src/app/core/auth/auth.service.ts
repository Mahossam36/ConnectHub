import { Injectable, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BffApiService } from '../services/bff-api.service';
import { BffAuthResponse, LoginRequest, RegisterRequest } from '../models/auth.models';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly googleStarting = signal(false);
  constructor(private readonly bffApi: BffApiService, private readonly session: SessionService) {}
  login(request: LoginRequest): Observable<BffAuthResponse> { return this.bffApi.post<BffAuthResponse>(environment.bff.auth.loginPath, request).pipe(tap((r) => this.establish(r))); }
  register(request: RegisterRequest): Observable<BffAuthResponse> { return this.bffApi.post<BffAuthResponse>(environment.bff.auth.registerPath, request).pipe(tap((r) => this.establish(r))); }
  restoreSession(): Observable<boolean> {
    return this.bffApi.get<BffAuthResponse>('/auth/me').pipe(
      tap((response) => this.establish(response)),
      map(() => true),
      catchError(() => {
        this.session.clear();
        return of(false);
      })
    );
  }
  startGoogleAuthentication(): void {
    if (!environment.bff.auth.googlePath) throw { message: 'Google sign-in is not configured yet.' };
    this.googleStarting.set(true);
    window.location.assign(`${environment.bff.baseUrl.replace(/\/$/, '')}/${environment.bff.auth.googlePath.replace(/^\//, '')}`);
  }
  logout(): Observable<unknown> { return this.bffApi.post(environment.bff.auth.logoutPath).pipe(tap(() => this.session.clear())); }
  clearSession(): void { this.session.clear(); }
  private establish(response: BffAuthResponse): void {
    this.session.establish({ id: response.sessionId, user: response.user ?? { id: response.userId ?? '', email: response.email ?? '', displayName: response.displayName ?? '', avatarUrl: response.avatarUrl } });
  }
}
