# ADR-0005: RxJS Reactive State Management

**Date:** 2026-04-01
**Category:** frontend
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Toromont Fleet Hub requires robust asynchronous data handling for several real-time and interactive features: telemetry dashboards that auto-refresh on a 60-second interval, SignalR-based push notifications for alerts and work order updates, HTTP API calls to the ASP.NET Core backend with MSAL token injection, and user interactions that trigger cascading data updates across components. The application needs a state management strategy that handles these async flows cleanly while remaining maintainable and testable. The current application complexity involves approximately 6-8 feature areas with moderate inter-feature communication needs.

## Decision

Use a hybrid state management approach: NgRx Store for authentication and global user state (AuthStateService), and RxJS observables with BehaviorSubject-based Angular services for feature-level state management (equipment, work orders, telemetry, parts, etc.). HttpClient with MsalInterceptor handles authenticated API communication with automatic token injection. NgRx provides the enforced unidirectional data flow needed for security-critical auth state, while RxJS services keep feature-level state simple and lightweight.

## Options Considered

### Option 1: Hybrid NgRx + RxJS Services (chosen)

- **Pros:**
  - RxJS is already included as a core Angular dependency; no additional library needed
  - NgRx Store provides enforced unidirectional data flow for security-critical authentication state (current user, role, organization context)
  - BehaviorSubjects in services provide component-scoped or feature-scoped state with current-value semantics
  - Operators like `switchMap`, `debounceTime`, `combineLatest`, and `retry` handle complex async patterns elegantly
  - Sufficient for the current application complexity without introducing unnecessary abstraction layers
  - Lower learning curve than full Redux-pattern libraries
  - Services are simple to unit test with TestScheduler and marble testing
  - HttpClient returns observables natively, integrating seamlessly with the reactive data flow
  - Can be incrementally upgraded to NgRx if application complexity grows significantly

- **Cons:**
  - No enforced unidirectional data flow; discipline is required to avoid state mutation anti-patterns
  - No built-in dev tools for state inspection (unlike NgRx DevTools or Redux DevTools)
  - Lack of standardized patterns may lead to inconsistent state management across feature areas
  - Complex multi-source state derivations can become hard to trace without a centralized store

### Option 2: NgRx Store for Everything

- **Pros:**
  - Enforced unidirectional data flow with actions, reducers, and selectors
  - Excellent dev tools for time-travel debugging and state inspection
  - Well-documented patterns for side effects via NgRx Effects
  - Strong community and ecosystem (entity adapter, router store, component store)

- **Cons:**
  - Significant boilerplate: actions, reducers, selectors, effects for every state slice
  - Steep learning curve for developers unfamiliar with Redux patterns
  - Overkill for the current application complexity where most state is feature-local
  - Additional bundle size for the NgRx packages

### Option 3: Akita

- **Pros:**
  - Less boilerplate than NgRx with a simpler store/query pattern
  - Built-in entity management and caching
  - Dev tools available for state inspection

- **Cons:**
  - Smaller community and less active maintenance than NgRx
  - Additional dependency to manage and keep updated
  - Less Angular ecosystem alignment than NgRx

### Option 4: NGXS

- **Pros:**
  - Decorator-based approach reduces boilerplate compared to NgRx
  - Closer to Angular's class-based patterns
  - Plugin system for common features (router, storage, WebSocket)

- **Cons:**
  - Smaller community than NgRx
  - Still adds Redux-like ceremony that may be unnecessary for current complexity
  - Additional dependency and learning curve

### Option 5: Angular Signals Only

- **Pros:**
  - Built into Angular 17 with no additional dependencies
  - Simple, synchronous reactive primitive
  - Fine-grained change detection integration

- **Cons:**
  - Signals alone do not handle async operations (HTTP calls, WebSocket streams, timers)
  - Interop with RxJS is still needed for HttpClient, SignalR, and interval-based refresh
  - Signal-based patterns for complex async flows are not yet mature in the Angular ecosystem

## Consequences

### Positive

- No additional dependencies beyond what Angular already provides, keeping the bundle lean
- Developers work with RxJS patterns they already need to know for Angular's HttpClient, Router, and Forms
- BehaviorSubject-based services provide a clear and testable pattern for feature-level state
- The 60-second telemetry auto-refresh is naturally expressed with `interval()` piped through `switchMap` to the HTTP call
- SignalR hub connections integrate seamlessly as observable streams
- MsalInterceptor transparently handles token acquisition and injection for all HttpClient calls
- Incremental path to NgRx exists if application complexity grows beyond service-based management

### Negative

- Team must establish and enforce conventions for service-based state management to ensure consistency
- Debugging complex observable chains requires RxJS proficiency and careful use of `tap` operators for logging
- No time-travel debugging capability without implementing custom tooling
- Risk of memory leaks from unmanaged subscriptions if developers do not use `takeUntilDestroyed`, `async` pipe, or explicit unsubscription

### Risks

- As the application grows, service-based state management may become insufficient for complex cross-feature state coordination
- Inconsistent patterns may emerge across teams if coding guidelines are not established and enforced
- Developers may create deeply nested observable chains that become difficult to maintain and debug

## Implementation Notes

- Standard service-based state pattern:
  ```typescript
  @Injectable({ providedIn: 'root' })
  export class EquipmentService {
    private equipmentSubject = new BehaviorSubject<Equipment[]>([]);
    public equipment$ = this.equipmentSubject.asObservable();

    constructor(private http: HttpClient) {}

    loadEquipment(params: GridDataRequest): Observable<GridDataResult> {
      return this.http.get<GridDataResult>('/api/equipment', { params });
    }
  }
  ```
- Telemetry auto-refresh pattern:
  ```typescript
  telemetry$ = interval(60_000).pipe(
    startWith(0),
    switchMap(() => this.http.get<TelemetryData[]>('/api/telemetry')),
    retry({ count: 3, delay: 5_000 }),
    shareReplay(1)
  );
  ```
- SignalR integration as observable:
  ```typescript
  notifications$ = new Observable<Notification>(observer => {
    this.hubConnection.on('ReceiveNotification', (notification) => observer.next(notification));
    return () => this.hubConnection.off('ReceiveNotification');
  });
  ```
- Configure `provideHttpClient(withInterceptorsFromDi())` in `main.ts` to enable MsalInterceptor for automatic Bearer token injection on all API calls
- Use `takeUntilDestroyed()` from `@angular/core/rxjs-interop` in components to automatically unsubscribe on component destruction
- Prefer the `async` pipe in templates over manual subscriptions to avoid memory leaks
- Establish team convention: one state service per feature area (EquipmentService, WorkOrderService, TelemetryService, etc.)

## References

- L1-005: Telemetry and Monitoring requirement
- L1-008: Notifications and Alerts requirement
- RxJS documentation: https://rxjs.dev
- Angular HttpClient guide: https://angular.dev/guide/http
- Angular Signals interop: https://angular.dev/guide/signals/rxjs-interop
