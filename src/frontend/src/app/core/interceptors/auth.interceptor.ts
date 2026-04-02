import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { from, switchMap } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const msalService = inject(MsalService);
  const account = msalService.instance.getActiveAccount();

  if (!account) {
    return next(req);
  }

  return from(
    msalService.instance.acquireTokenSilent({
      scopes: ['api://fleet-hub-api/.default'],
      account
    })
  ).pipe(
    switchMap(result => {
      const authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${result.accessToken}`
        }
      });
      return next(authReq);
    })
  );
};
