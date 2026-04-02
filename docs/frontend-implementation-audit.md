# Frontend Implementation Audit vs Detailed Designs

Date: 2026-04-02

## Scope

This document compares the Angular frontend in `src/frontend/src/app` against the frontend-relevant portions of the detailed design set.

The comparison focused primarily on:

- `01-authentication`
- `02-equipment-management`
- `03-service-management`
- `04-parts-ordering`
- `05-telemetry-monitoring`
- `06-ai-insights`
- `07-notifications-reporting`
- `09-tenant-identity-hardening`

`08-mediatr-cqrs-refactor` and `10-telemetry-ingestion-redesign` are backend-oriented and were not treated as primary frontend scope, except where they affect frontend-facing contracts.

This was a static code audit. I did not run browser automation or perform pixel-level visual diffing against `docs/ui-design.pen`.

## Executive Summary

The frontend covers most major screens, but several important workflows are still either missing, partially wired, or implemented with placeholder logic that does not match the detailed designs.

The largest gaps are:

- Notifications are not fully wired. The bell badge exists, but the dropdown does not load notification history, mark-as-read is implemented against a nonexistent SignalR method, notification preferences are static and unrouted, and notification deep links point to routes that do not exist.
- Service management diverges significantly from the design. The work-order list is a custom table instead of the designed Kendo Grid, the calendar is not a Kendo Scheduler, work-order history falls back to fabricated data, and assigned technicians are hard-coded names sent as `assignedToUserId`.
- Parts ordering remains incomplete from a user-flow perspective. There is no order history page, cart totals are computed locally, the submit action posts to `/orders` instead of the designed `/parts/orders`, and the client fabricates a PO number if the server does not return one.

There are also broader implementation drifts:

- Equipment list/detail are functional but do not match the designed Kendo Grid plus tabbed detail view.
- Telemetry is visually close in the charts area, but the GPS panel is still a placeholder and the route parameter is ignored.
- Reports looks close to the design, but exports are likely broken and the “Equipment Lifecycle” card is wired to the wrong endpoint.
- AI Insights has KPI cards and a predictions table, but it is still a custom table without the designed filter/search behavior.

## Critical Findings

### 1. Notifications are not implemented according to the designed interaction model

The design expects a notification bell with unread count, mark-read flows, preferences management, and notification navigation. The current implementation only loads unread count initially. It never loads recent notifications into the dropdown, and `markAsRead()` invokes a SignalR hub method named `MarkAsRead` instead of using the designed REST API. `startConnection()` is defined but is not called anywhere in the app. The notification preferences screen exists only as a static in-memory component and is not routed. Notification click-through routes are also invalid for work orders and parts orders.

Impact: the notification bell is only partially functional, and several user flows described in the design are currently broken or unreachable.

Evidence:

- Design: `docs/detailed-designs/07-notifications-reporting/README.md:44-49`
- Design: `docs/detailed-designs/07-notifications-reporting/README.md:72`
- Frontend routes: `src/frontend/src/app/app.routes.ts:15-63`
- Notification service: `src/frontend/src/app/core/services/notification.service.ts:14-19`
- Notification service: `src/frontend/src/app/core/services/notification.service.ts:21-39`
- Notification service: `src/frontend/src/app/core/services/notification.service.ts:47-50`
- Header behavior: `src/frontend/src/app/shared/components/header/header.component.ts:27-57`
- Invalid deep links: `src/frontend/src/app/shared/components/header/header.component.ts:67-72`
- Static preferences only: `src/frontend/src/app/features/settings/notification-preferences.component.ts:23-32`
- Repo search shows `startConnection(` is only defined in `NotificationService` and never called elsewhere

### 2. Service management diverges substantially from the designed Kendo Grid + Scheduler workflow

The service design calls for a Kendo Grid list view and a Kendo Scheduler calendar view with day/week/month views and drag-drop rescheduling. The current implementation uses a custom HTML table for the list and a simple list of cards for the calendar. It does not call the designed `/work-orders/calendar?start=&end=` endpoint. Instead, it pulls `/work-orders` and reshapes the results locally. The detail modal tries to load `/work-orders/{id}/history`, a route not present in the backend design or current implementation, then fabricates fallback history if that fails. Status changes also update the UI optimistically even when the API call fails.

Impact: the service UI can present misleading state and does not implement the designed scheduling experience.

Evidence:

- Design: `docs/detailed-designs/03-service-management/README.md:35`
- Design: `docs/detailed-designs/03-service-management/README.md:56-58`
- `src/frontend/src/app/features/service/work-orders.component.html:2-33`
- `src/frontend/src/app/features/service/work-orders.component.html:58-99`
- `src/frontend/src/app/features/service/work-orders.component.ts:130-147`
- `src/frontend/src/app/features/service/work-orders.component.ts:184-208`
- `src/frontend/src/app/features/service/work-orders.component.ts:214-237`

### 3. Work-order creation uses placeholder technician data and sends invalid identity values

The create-work-order flow uses a hard-coded technician name list (`John Smith`, `Jane Doe`, etc.) instead of loading actual tenant users. That string value is then sent as `assignedToUserId`, even though the design and backend expect a user identifier. This is not just a UI simplification; it changes the request contract.

Impact: assigned-technician workflows cannot be trusted, and the frontend payload is incompatible with the designed identity model.

Evidence:

- Design: `docs/detailed-designs/03-service-management/README.md:46`
- Design: `docs/detailed-designs/03-service-management/README.md:86`
- `src/frontend/src/app/features/service/work-orders.component.ts:69-80`
- `src/frontend/src/app/features/service/work-orders.component.ts:275-292`
- `src/frontend/src/app/features/service/work-orders.component.html:216-228`

## High Findings

### 4. Parts ordering is missing the designed order-history workflow and uses the wrong submit contract

The parts design includes a cart plus an `OrderHistoryComponent` for past orders. The current route table only exposes `/parts` and `/parts/cart`; there is no order-history page. The cart submits orders to `/orders`, not `/parts/orders`, and if the response omits `orderNumber`, the client generates a random `PO-...` string locally.

Impact: the ordering UX does not match the documented flow, and the order-confirmation number may not be authoritative.

Evidence:

- Design: `docs/detailed-designs/04-parts-ordering/README.md:49`
- Design: `docs/detailed-designs/04-parts-ordering/README.md:65`
- `src/frontend/src/app/app.routes.ts:35-40`
- `src/frontend/src/app/features/parts/cart.component.ts:107-120`

### 5. Cart state and totals are more local than the design allows

The design says cart prices and totals are server-authoritative. The current cart screen loads raw cart items and computes subtotal locally from `item.part.price`. The parts catalog also increments `cartCount` locally rather than loading or syncing actual cart state from the server.

Impact: cart totals and cart badge state can drift from the authoritative backend state.

Evidence:

- Design: `docs/detailed-designs/04-parts-ordering/README.md:96-100`
- `src/frontend/src/app/features/parts/cart.component.ts:50-69`
- `src/frontend/src/app/features/parts/parts-catalog.component.ts:175-186`
- `src/frontend/src/app/features/parts/parts-catalog.component.html:5-9`

### 6. Equipment list/detail do not match the designed Kendo Grid and tabbed detail view

The design explicitly calls for a Kendo Grid with server-side pagination, sorting, and filtering, plus a tabbed equipment detail page. The current list page fetches up to 1000 rows, then sorts and paginates client-side in a custom HTML table. The detail page is a fixed two-column layout with no tabs. The “Edit” button routes back to the list, and the map is still a placeholder.

Impact: the implementation is functional, but it is not the designed interaction model and will not scale or behave like the documented UI.

Evidence:

- Design: `docs/detailed-designs/02-equipment-management/README.md:15`
- Design: `docs/detailed-designs/02-equipment-management/README.md:59-62`
- `src/frontend/src/app/features/equipment/equipment-list.component.ts:71-123`
- `src/frontend/src/app/features/equipment/equipment-list.component.html` uses a plain `<table>` for the desktop grid
- `src/frontend/src/app/features/equipment/equipment-detail.component.html:23-88`
- `src/frontend/src/app/features/equipment/equipment-detail.component.html:17-19`
- `src/frontend/src/app/features/equipment/equipment-detail.component.html:65-67`

### 7. Equipment management is missing import/decommission UX from the design

The equipment feature design includes legacy XML import and decommission behavior with confirmation. The current equipment list page offers “Add Equipment” only. There is no XML import entry point, no import result UI, and no decommission action or confirmation flow in the detail or list view.

Impact: the frontend does not expose the full equipment-management feature set described in the design.

Evidence:

- Design: `docs/detailed-designs/02-equipment-management/README.md:41`
- Design: `docs/detailed-designs/02-equipment-management/README.md:107-113`
- Design: `docs/detailed-designs/02-equipment-management/README.md:181`
- `src/frontend/src/app/features/equipment/equipment-list.component.html:2-223`
- `src/frontend/src/app/features/equipment/equipment-detail.component.html:10-95`

### 8. Telemetry dashboard ignores the routed equipment id and still ships a placeholder GPS panel

The app defines `/telemetry/:equipmentId`, but the telemetry component does not read the route parameter. On init it loads the equipment list, injects a fake “No Data Equipment” option, and selects the first equipment automatically. The GPS panel is still a “Leaflet integration coming soon” placeholder, and the time-range control omits the designed custom range option.

Impact: deep links from equipment detail do not reliably open the requested equipment, and the GPS trail experience is not implemented.

Evidence:

- Design: `docs/detailed-designs/05-telemetry-monitoring/README.md:60-63`
- Design: `docs/detailed-designs/05-telemetry-monitoring/README.md:178`
- `src/frontend/src/app/app.routes.ts:43-48`
- `src/frontend/src/app/features/telemetry/telemetry-dashboard.component.ts:68-82`
- `src/frontend/src/app/features/telemetry/telemetry-dashboard.component.ts:101-108`
- `src/frontend/src/app/features/telemetry/telemetry-dashboard.component.html:147-160`

### 9. Reports page is visually close, but one report card is wired incorrectly and export handling is likely broken

The “Equipment Lifecycle” report card is present in the UI, but it is wired to the fleet-utilization endpoint instead of its own report implementation. Separately, `ReportsComponent.exportReport()` treats the response as `Blob`, but `ApiService.post()` always uses Angular’s default JSON response handling and does not set `responseType: 'blob'`.

Impact: lifecycle reporting is misleading, and export buttons for PDF/Excel/CSV are likely not reliable.

Evidence:

- Design: `docs/detailed-designs/07-notifications-reporting/README.md:57-60`
- `src/frontend/src/app/features/reports/reports.component.ts:36-40`
- `src/frontend/src/app/features/reports/reports.component.ts:127-149`
- `src/frontend/src/app/core/services/api.service.ts:24-27`

### 10. AI Insights is missing the designed grid behaviors and semantic search workflow

The AI Insights design expects a Kendo Grid with sorting, filtering, and pagination, plus semantic search support. The current implementation uses a custom HTML table with client-side sorting and pagination only. There is no search UI or semantic-search flow at all. The dismiss logic also decrements “high priority” based on `confidenceScore` instead of using the prediction priority field from the design.

Impact: the page covers the broad layout but not the intended data interaction model.

Evidence:

- Design: `docs/detailed-designs/06-ai-insights/README.md:62`
- Design: `docs/detailed-designs/06-ai-insights/README.md:67-69`
- `src/frontend/src/app/features/ai-insights/ai-insights.component.ts:42-65`
- `src/frontend/src/app/features/ai-insights/ai-insights.component.ts:109-117`
- `src/frontend/src/app/features/ai-insights/ai-insights.component.html:36-95`

## Medium Findings

### 11. User-management and identity UX only partially cover the design

The login page broadly matches the split-panel design. The user-management screen also exists and supports invite, role editing, and activation/deactivation. However, it uses a plain HTML table instead of the designed Kendo Grid, and there is no `accept-invite` route or page in the frontend. The newer tenant-hardening design also expects an organization switcher in the header for multi-org users, but the header has no such UI.

Impact: basic admin user management exists, but the identity lifecycle and multi-org UX are incomplete.

Evidence:

- Design: `docs/detailed-designs/01-authentication/README.md:7`
- Design: `docs/detailed-designs/01-authentication/README.md:63`
- Design: `docs/detailed-designs/01-authentication/README.md:69`
- Design: `docs/detailed-designs/09-tenant-identity-hardening/README.md:379-405`
- `src/frontend/src/app/app.routes.ts:5-74`
- `src/frontend/src/app/features/users/user-management.component.ts:42-112`
- `src/frontend/src/app/features/login/login.component.html:2-40`
- `src/frontend/src/app/shared/components/header/header.component.html` shows only search, notification bell, and user menu

### 12. Tenant switching does not match the hardening design

The tenant-hardening design expects active-organization switching via `X-Active-Organization` plus a visible org switcher. The current frontend has no organization-switch UI and the interceptor uses Azure AD `tenantId` to set `X-Tenant-Id`, which is not the same concept as the active Fleet Hub organization.

Impact: the frontend is not aligned with the multi-organization model described in the latest tenant design.

Evidence:

- Design: `docs/detailed-designs/09-tenant-identity-hardening/README.md:59`
- Design: `docs/detailed-designs/01-authentication/README.md:172`
- `src/frontend/src/app/core/interceptors/tenant.interceptor.ts:14-17`
- `src/frontend/src/app/core/services/auth.service.ts:34-35`

### 13. Dashboard contains placeholder map content and a broken alerts link

The dashboard map uses hard-coded marker positions and placeholder copy (“Leaflet integration”). The alerts panel links to `/alerts`, but there is no `/alerts` route in the application route table.

Impact: the dashboard presents non-production placeholder behavior and at least one broken navigation path.

Evidence:

- `src/frontend/src/app/features/dashboard/dashboard.component.ts:46-52`
- `src/frontend/src/app/features/dashboard/dashboard.component.html:33-47`
- `src/frontend/src/app/features/dashboard/dashboard.component.html:58`
- `src/frontend/src/app/app.routes.ts:15-63`

## Areas That Already Partially Align

- The login screen broadly matches the split branding / sign-in layout described in the authentication design.
- The header bell, KPI card pattern, and report page structure are directionally consistent with the design set.
- Telemetry, reports, and AI Insights already use Kendo controls in some areas, so the missing pieces are concentrated in specific workflows rather than the entire frontend stack.
- Equipment, service, parts, telemetry, AI, reports, and admin routes all exist at a basic level, which means most gaps are refinements or missing interactions rather than total screen absence.

## Recommended Remediation Order

1. Fix notifications first. Wire the bell to the designed REST endpoints, load recent notifications on open, add a real preferences screen/route, and remove invalid deep links.
2. Rebuild service management around the designed grid and scheduler behavior. Replace hard-coded technicians with real users and stop fabricating history/status state on failed API calls.
3. Normalize the parts ordering flow. Add order history, submit against the designed endpoint shape, and remove client-generated PO numbers.
4. Align equipment and telemetry detail flows next, especially tabbed detail, import/decommission actions, route-parameter handling, and the map placeholders.
5. Correct reports export handling and either implement or remove the equipment-lifecycle card until a real backend contract exists.
6. Finish the identity/multi-org UX by adding accept-invite and the organization switcher described in the tenant-hardening design.
