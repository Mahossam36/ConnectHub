import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError, timeout, TimeoutError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiError } from '../models/auth.models';

// Hard ceiling on any single BFF call. If the backend hangs (deadlock,
// slow query, stuck CORS preflight, etc.) the request now fails after
// this many ms instead of leaving callers (e.g. HomeComponent's
// isLoading flag) stuck forever.
const REQUEST_TIMEOUT_MS = 15000;

@Injectable({ providedIn: 'root' })
export class BffApiService {
  constructor(private readonly http: HttpClient) {}

  get<TResponse>(path: string): Observable<TResponse> {
    if (!path)
      return throwError(
        () => ({ message: 'This BFF endpoint has not been configured yet.' }) satisfies ApiError,
      );
    const url = `${environment.bff.baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;
    return this.http
      .get<TResponse>(url, { withCredentials: environment.bff.useCookieSession })
      .pipe(
        timeout(REQUEST_TIMEOUT_MS),
        catchError((error) => this.toApiError(error)),
      );
  }

  /**
   * Fetches a binary resource (images, videos, documents served through
   * /api/Attachments/{id}) as a Blob, using the same authenticated,
   * cookie-credentialed HttpClient as every other call.
   *
   * This is deliberately NOT just a plain <img src="..."> URL: those
   * protected endpoints require the session cookie, and browsers do not
   * reliably send cookies on cross-origin <img>/<video> subresource
   * requests (SameSite policy). Routing the request through HttpClient
   * guarantees the cookie is attached, exactly like our JSON calls.
   */
  getBlob(path: string): Observable<Blob> {
    if (!path)
      return throwError(
        () => ({ message: 'This BFF endpoint has not been configured yet.' }) satisfies ApiError,
      );
    const url = `${environment.bff.baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;
    return this.http
      .get(url, { withCredentials: environment.bff.useCookieSession, responseType: 'blob' })
      .pipe(
        timeout(REQUEST_TIMEOUT_MS),
        catchError((error) => this.toApiError(error)),
      );
  }

  post<TResponse>(path: string, body?: unknown): Observable<TResponse> {
    if (!path)
      return throwError(
        () => ({ message: 'This BFF endpoint has not been configured yet.' }) satisfies ApiError,
      );
    const url = `${environment.bff.baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;
    return this.http
      .post<TResponse>(url, body, { withCredentials: environment.bff.useCookieSession })
      .pipe(
        timeout(REQUEST_TIMEOUT_MS),
        catchError((error) => this.toApiError(error)),
      );
  }

  put<TResponse>(path: string, body?: unknown): Observable<TResponse> {
    if (!path)
      return throwError(
        () => ({ message: 'This BFF endpoint has not been configured yet.' }) satisfies ApiError,
      );
    const url = `${environment.bff.baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;
    return this.http
      .put<TResponse>(url, body, { withCredentials: environment.bff.useCookieSession })
      .pipe(
        timeout(REQUEST_TIMEOUT_MS),
        catchError((error) => this.toApiError(error)),
      );
  }

  delete<TResponse>(path: string): Observable<TResponse> {
    if (!path)
      return throwError(
        () => ({ message: 'This BFF endpoint has not been configured yet.' }) satisfies ApiError,
      );
    const url = `${environment.bff.baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;
    return this.http
      .delete<TResponse>(url, { withCredentials: environment.bff.useCookieSession })
      .pipe(
        timeout(REQUEST_TIMEOUT_MS),
        catchError((error) => this.toApiError(error)),
      );
  }

  postForm<TResponse>(path: string, body: FormData): Observable<TResponse> {
    if (!path)
      return throwError(
        () => ({ message: 'This BFF endpoint has not been configured yet.' }) satisfies ApiError,
      );
    const url = `${environment.bff.baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;
    // File uploads legitimately take longer, so give them more headroom.
    return this.http
      .post<TResponse>(url, body, { withCredentials: environment.bff.useCookieSession })
      .pipe(
        timeout(REQUEST_TIMEOUT_MS * 4),
        catchError((error) => this.toApiError(error)),
      );
  }

  private toApiError(error: unknown): Observable<never> {
    if (error instanceof TimeoutError) {
      return throwError(
        () =>
          ({
            status: 0,
            message: 'The request took too long to respond. Please try again.',
          }) satisfies ApiError,
      );
    }

    const httpError = error as HttpErrorResponse;
    const payload = httpError.error as {
      detail?: string;
      title?: string;
      errors?: Record<string, string[]>;
    } | null;

    return throwError(
      () =>
        ({
          status: httpError.status,
          message:
            httpError.status === 0
              ? 'We could not reach Yalla. Please check your connection and try again.'
              : (payload?.detail ?? payload?.title ?? 'Something went wrong. Please try again.'),
          fieldErrors: payload?.errors,
        }) satisfies ApiError,
    );
  }
}
