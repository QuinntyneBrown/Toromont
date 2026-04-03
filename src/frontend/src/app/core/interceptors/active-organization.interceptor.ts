import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ActiveOrganizationService } from '../services/active-organization.service';

export const activeOrganizationInterceptor: HttpInterceptorFn = (req, next) => {
  const orgService = inject(ActiveOrganizationService);
  const orgId = orgService.currentOrganizationId;

  if (orgId) {
    const clonedReq = req.clone({
      setHeaders: {
        'X-Active-Organization': orgId
      }
    });
    return next(clonedReq);
  }

  return next(req);
};
