import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MsalService } from '@azure/msal-angular';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
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

  return next(req);
};
