import { Injectable, inject } from '@angular/core';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { InteractionStatus } from '@azure/msal-browser';
import { Observable, of } from 'rxjs';
import { filter, map, switchMap } from 'rxjs/operators';
import { User } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private msalService = inject(MsalService);
  private msalBroadcastService = inject(MsalBroadcastService);

  get isAuthenticated$(): Observable<boolean> {
    return this.msalBroadcastService.inProgress$.pipe(
      filter((status: InteractionStatus) => status === InteractionStatus.None),
      switchMap(() => {
        const accounts = this.msalService.instance.getAllAccounts();
        return of(accounts.length > 0);
      })
    );
  }

  get user$(): Observable<User | null> {
    return this.isAuthenticated$.pipe(
      map(isAuth => {
        if (!isAuth) return null;
        const account = this.msalService.instance.getActiveAccount();
        if (!account) return null;
        return {
          id: account.localAccountId,
          email: account.username,
          firstName: account.name?.split(' ')[0] ?? '',
          lastName: account.name?.split(' ').slice(1).join(' ') ?? '',
          role: (account.idTokenClaims?.['extension_Role'] as User['role']) ?? 'ReadOnly',
          tenantId: account.tenantId,
          isActive: true,
          createdAt: ''
        } as User;
      })
    );
  }

  login(): void {
    this.msalService.loginRedirect();
  }

  logout(): void {
    this.msalService.logoutRedirect();
  }
}
