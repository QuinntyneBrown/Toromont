import { inject, isDevMode } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, take, timeout, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.isAuthenticated$.pipe(
    timeout(isDevMode() ? 100 : 3000),
    take(1),
    map(isAuthenticated => {
      if (isAuthenticated) return true;
      if (isDevMode()) return true; // Allow access in dev mode when MSAL is not configured
      router.navigate(['/login']);
      return false;
    }),
    catchError(() => {
      // MSAL timed out (not configured) — allow access in dev mode
      if (isDevMode()) return of(true);
      router.navigate(['/login']);
      return of(false);
    })
  );
};
