import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { SessionService } from '../auth/session.service';

export const authGuard: CanActivateFn = (_, state) => inject(SessionService).isAuthenticated()
  ? true
  : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
