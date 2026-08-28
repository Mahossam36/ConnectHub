import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { SessionService } from '../auth/session.service';

export const bffSessionInterceptor: HttpInterceptorFn = (request, next) => {
  const headerName = environment.bff.sessionHeaderName;
  const sessionId = inject(SessionService).sessionId();
  if (!headerName || !sessionId || !request.url.startsWith(environment.bff.baseUrl)) return next(request);
  return next(request.clone({ setHeaders: { [headerName]: sessionId }, withCredentials: environment.bff.useCookieSession }));
};
