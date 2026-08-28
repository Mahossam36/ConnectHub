import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiError } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class BffApiService {
  constructor(private readonly http: HttpClient) {}
  get<TResponse>(path: string): Observable<TResponse> {
    if (!path) return throwError(() => ({ message: 'This BFF endpoint has not been configured yet.' } satisfies ApiError));
    const url = `${environment.bff.baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;
    return this.http.get<TResponse>(url, { withCredentials: environment.bff.useCookieSession }).pipe(catchError((error) => this.toApiError(error)));
  }
  post<TResponse>(path: string, body?: unknown): Observable<TResponse> {
    if (!path) return throwError(() => ({ message: 'This BFF endpoint has not been configured yet.' } satisfies ApiError));
    const url = `${environment.bff.baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;
    return this.http.post<TResponse>(url, body, { withCredentials: environment.bff.useCookieSession }).pipe(catchError((error) => this.toApiError(error)));
  }
  private toApiError(error: HttpErrorResponse): Observable<never> {
    const payload = error.error as { detail?: string; title?: string; errors?: Record<string, string[]> } | null;
    return throwError(() => ({
      status: error.status,
      message: error.status === 0 ? 'We could not reach Yalla. Please check your connection and try again.' : payload?.detail ?? payload?.title ?? 'Something went wrong. Please try again.',
      fieldErrors: payload?.errors
    } satisfies ApiError));
  }
}
