import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { ApiService } from './api.service';

export interface Membership {
  organizationId: string;
  organizationName: string;
  isDefault: boolean;
  isActive: boolean;
}

export interface UserInfo {
  id: string;
  email: string;
  displayName: string;
  role: string;
}

export interface CurrentUserContext {
  user: UserInfo;
  activeOrganizationId: string;
  memberships: Membership[];
}

@Injectable({ providedIn: 'root' })
export class ActiveOrganizationService {
  private api = inject(ApiService);

  private contextSubject = new BehaviorSubject<CurrentUserContext | null>(null);

  context$ = this.contextSubject.asObservable();

  get activeOrganizationId$(): Observable<string | null> {
    return new Observable(subscriber => {
      this.context$.subscribe(ctx => {
        subscriber.next(ctx?.activeOrganizationId ?? null);
      });
    });
  }

  get currentOrganizationId(): string | null {
    return this.contextSubject.value?.activeOrganizationId ?? null;
  }

  loadContext(): Observable<CurrentUserContext> {
    return this.api.get<CurrentUserContext>('/me/context').pipe(
      tap(ctx => this.contextSubject.next(ctx))
    );
  }

  setActiveOrganization(organizationId: string): Observable<CurrentUserContext> {
    return this.api.put<CurrentUserContext>('/me/active-organization', { organizationId }).pipe(
      tap(ctx => this.contextSubject.next(ctx))
    );
  }
}
