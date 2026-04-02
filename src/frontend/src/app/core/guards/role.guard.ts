import { inject, isDevMode } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { map, take, timeout, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const requiredRoles = route.data['roles'] as string[];

  return authService.user$.pipe(
    timeout(isDevMode() ? 100 : 3000),
    take(1),
    map(user => {
      if (user && requiredRoles.includes(user.role)) return true;
      if (isDevMode()) return true; // Allow access in dev mode
      router.navigate(['/dashboard']);
      return false;
    }),
    catchError(() => {
      if (isDevMode()) return of(true);
      router.navigate(['/dashboard']);
      return of(false);
    })
  );
};
