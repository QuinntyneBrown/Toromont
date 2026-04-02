import { HttpInterceptorFn } from '@angular/common/http';
import { inject, isDevMode } from '@angular/core';
import { MsalService } from '@azure/msal-angular';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  if (isDevMode()) {
    return next(req);
  }

  try {
    const msalService = inject(MsalService);
    const account = msalService.instance.getActiveAccount();

    if (account?.tenantId) {
      const tenantReq = req.clone({
        setHeaders: {
          'X-Tenant-Id': account.tenantId
        }
      });
      return next(tenantReq);
    }
  } catch {
    // MSAL not available
  }

  return next(req);
};
