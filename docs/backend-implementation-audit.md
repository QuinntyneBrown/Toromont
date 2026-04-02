# Backend Implementation Audit vs Detailed Designs

Date: 2026-04-02

## Scope

This document compares the current backend implementation in `src/backend/IronvaleFleetHub.Api` against the detailed design set in `docs/detailed-designs/01-*` through `08-*`.

This was a static code audit. I did not execute end-to-end backend scenarios. The design set is still marked Draft, so this document calls out both implementation gaps and a small number of design ambiguities that affect the comparison.

## Executive Summary

The backend has broad endpoint coverage for the main product areas, but it does not yet match the detailed designs in several important ways.

The largest gaps are cross-cutting:

- The MediatR/CQRS refactor described in Feature 08 has not been implemented. Controllers still contain business logic directly, and the pipeline behaviors described in the design are not registered.
- The authentication and tenant model is simpler than the design and does not implement the documented multi-organization membership model, missing-org failure behavior, or immediate revocation behavior for deactivated users.
- The telemetry pipeline is implemented as a normal API controller with EF Core inserts and an API key, not as the Azure Function plus Dapper ingestion pipeline described in the design.

Feature-specific gaps are also substantial:

- Equipment detail, XML import validation, pagination, and API contracts differ from the design.
- Work order, parts, AI insights, notifications, alerts, and reporting all have route, payload, authorization, or behavior drift relative to the documented contracts.

## Critical Findings

### 1. MediatR/CQRS refactor is not implemented

The detailed design for Feature 08 expects thin controllers that delegate to MediatR request handlers, with validation and logging enforced through pipeline behaviors. The current implementation still places request validation, orchestration, and persistence directly inside controllers. `Program.cs` registers MediatR only with `RegisterServicesFromAssemblyContaining<Program>()` and does not register `LoggingBehavior`, `ValidationBehavior`, or FluentValidation validators from the assembly.

Impact: the architecture does not match the documented vertical-slice design, cross-cutting validation/logging is not centralized, and the planned CQRS separation has not started.

Evidence:

- `docs/detailed-designs/08-mediatr-cqrs-refactor/README.md:9-19`
- `docs/detailed-designs/08-mediatr-cqrs-refactor/README.md:92-101`
- `docs/detailed-designs/08-mediatr-cqrs-refactor/README.md:357-460`
- `src/backend/IronvaleFleetHub.Api/Program.cs:59-63`
- `src/backend/IronvaleFleetHub.Api/Controllers/EquipmentController.cs:101-167`
- `src/backend/IronvaleFleetHub.Api/Controllers/WorkOrdersController.cs:104-167`
- `src/backend/IronvaleFleetHub.Api/Controllers/OrdersController.cs:27-75`

### 2. Tenant and identity model diverge from the authentication design

The authentication design expects `TenantContextMiddleware` to enforce tenant resolution, return 403 when the org claim is missing or inactive, and support the resolved active organization model. It also resolves the multi-organization question in favor of a `UserOrganization` join table with active-org switching. The current backend stores only a single `OrganizationId` on `User`, has no `UserOrganization` type, and the middleware simply continues when tenant resolution fails. It also supports header-based tenant override as a last resort, which is not part of the design.

Impact: tenant behavior is weaker and materially different from the design. The implementation cannot support the documented multi-org experience, and missing-org/inactive-org cases are not enforced as specified.

Evidence:

- `docs/detailed-designs/01-authentication/README.md:44-50`
- `docs/detailed-designs/01-authentication/README.md:68-70`
- `docs/detailed-designs/01-authentication/README.md:119-122`
- `docs/detailed-designs/01-authentication/README.md:172-174`
- `src/backend/IronvaleFleetHub.Api/Middleware/TenantContextMiddleware.cs:17-64`
- `src/backend/IronvaleFleetHub.Api/Models/User.cs:6-11`
- repo search for `UserOrganization` returned no matches

### 3. Telemetry ingestion architecture does not match the design

Feature 05 describes an Azure Function at `POST /api/telemetry` using Dapper bulk inserts, retry, and dead-letter handling. The current implementation exposes `POST /api/v1/telemetry/ingest` on the API itself, allows anonymous access with `X-Api-Key`, falls back to a hard-coded default key if the environment variable is missing, inserts a single EF Core entity, and immediately calls the alert evaluator in-process.

Impact: the ingestion path does not match the documented deployment model, throughput model, or failure handling strategy. This is the largest feature-level architecture delta in the backend.

Evidence:

- `docs/detailed-designs/05-telemetry-monitoring/README.md:31-37`
- `docs/detailed-designs/05-telemetry-monitoring/README.md:89-94`
- `docs/detailed-designs/05-telemetry-monitoring/README.md:177`
- `src/backend/IronvaleFleetHub.Api/Controllers/TelemetryController.cs:32-74`

## High Findings

### 4. User management only partially matches the authentication API

The design expects invitation sending plus a public `POST /api/v1/users/accept-invite` callback. The current controller implements list, invite, role update, deactivate, and activate, but there is no accept-invite endpoint. The deactivation flow also does not implement the documented immediate token revocation blacklist behavior.

Impact: the invitation lifecycle is incomplete relative to the design, and deactivated users are not handled the way the design specifies.

Evidence:

- `docs/detailed-designs/01-authentication/README.md:68-70`
- `docs/detailed-designs/01-authentication/README.md:174`
- `src/backend/IronvaleFleetHub.Api/Controllers/UsersController.cs:27-142`

### 5. Equipment management contracts and import behavior drift from the design

The equipment detail design expects latest telemetry and recent service history in the detail payload. The current `GET /api/v1/equipment/{id}` returns only the `Equipment` entity. The list endpoint uses `skip` and `take` rather than the documented `page`, `pageSize`, `sortBy`, and `sortDir` contract. The XML import path loads XML directly with `XDocument.LoadAsync` and returns only `{ Imported = count }`; it does not perform the documented XSD validation, XXE hardening, or detailed error summary response.

Impact: the published API contract for equipment list/detail/import does not match the design and the import implementation is materially simpler than the design.

Evidence:

- `docs/detailed-designs/02-equipment-management/README.md:34`
- `docs/detailed-designs/02-equipment-management/README.md:38`
- `docs/detailed-designs/02-equipment-management/README.md:53-54`
- `docs/detailed-designs/02-equipment-management/README.md:107-113`
- `docs/detailed-designs/02-equipment-management/README.md:171`
- `src/backend/IronvaleFleetHub.Api/Controllers/EquipmentController.cs:27-98`
- `src/backend/IronvaleFleetHub.Api/Controllers/EquipmentController.cs:206-267`

### 6. Work order authorization and side effects are lighter than designed

The service-management design expects role-specific write access, explicit status transition rules, and notification side effects on create and transition. The current controller is protected only with `[Authorize]` at the controller level, does not apply per-action RBAC policies for create/update, and performs no SignalR or notification-dispatch side effects. The transition map also includes `Cancelled`, which is not part of the documented status flow.

Impact: behavior is simpler than the design and some documented authorization and workflow rules are not enforced.

Evidence:

- `docs/detailed-designs/03-service-management/README.md:11`
- `docs/detailed-designs/03-service-management/README.md:33`
- `docs/detailed-designs/03-service-management/README.md:38-47`
- `docs/detailed-designs/03-service-management/README.md:163`
- `src/backend/IronvaleFleetHub.Api/Controllers/WorkOrdersController.cs:12-13`
- `src/backend/IronvaleFleetHub.Api/Controllers/WorkOrdersController.cs:104-167`
- `src/backend/IronvaleFleetHub.Api/Controllers/WorkOrdersController.cs:221-235`

### 7. Parts catalog, cart, and order APIs do not follow the documented route and workflow contracts

The parts design expects `/api/v1/parts/catalog`, `/api/v1/parts/catalog/search`, and `/api/v1/parts/orders`. The implementation exposes `/api/v1/parts`, `/api/v1/parts/search`, and `/api/v1/orders`. Natural-language search is implemented as tokenized `Contains` filtering instead of embeddings plus semantic ranking. Cart retrieval returns raw `CartItem` entities rather than the documented cart-with-totals contract, and add/update actions do not validate stock availability. Order numbers use `ORD-YYYYMMDD-NNN` rather than the documented `PO-YYYYMMDD-NNN`, and order submission does not show the designed transactional/notification workflow.

Impact: both API shape and business behavior are materially different from the design.

Evidence:

- `docs/detailed-designs/04-parts-ordering/README.md:31-33`
- `docs/detailed-designs/04-parts-ordering/README.md:40-50`
- `docs/detailed-designs/04-parts-ordering/README.md:96-99`
- `docs/detailed-designs/04-parts-ordering/README.md:105-108`
- `docs/detailed-designs/04-parts-ordering/README.md:152-180`
- `src/backend/IronvaleFleetHub.Api/Controllers/PartsController.cs:27-123`
- `src/backend/IronvaleFleetHub.Api/Controllers/CartController.cs:27-105`
- `src/backend/IronvaleFleetHub.Api/Controllers/OrdersController.cs:12-43`
- `src/backend/IronvaleFleetHub.Api/Controllers/OrdersController.cs:45-86`

### 8. AI Insights endpoints exist, but several routes and response contracts differ from the design

The AI design expects `GET /api/v1/ai/predictions?equipmentId=`, `GET /api/v1/ai/search?q=`, paginated/sortable contract shapes, and a dashboard stats payload with fields such as `highPriorityCount`, `anomaliesDetected`, and `estimatedCostSavings`. The implementation instead exposes `GET /api/v1/ai/predictions/{equipmentId}`, has no semantic search endpoint, returns raw entity lists, and returns dashboard stats with different property names (`TotalPredictions`, `HighPriority`, `AnomalyCount`, `EstimatedSavings`). Dismiss also returns the mutated entity rather than the documented contract with dismissal metadata.

Impact: the frontend/API contract for the AI area does not match the detailed design.

Evidence:

- `docs/detailed-designs/06-ai-insights/README.md:53-64`
- `docs/detailed-designs/06-ai-insights/README.md:159-181`
- `src/backend/IronvaleFleetHub.Api/Controllers/AIInsightsController.cs:26-124`

### 9. Notifications and reporting are only partially implemented to the design depth

The good news is that the NotificationHub already supports `org-{organizationId}` and `user-{userId}` groups. The remaining gap is in delivery orchestration and reporting internals. `NotificationsController` uses `PUT /api/v1/notifications/read-all` instead of the designed `mark-all-read` route. `NotificationDispatchService` stores the notification and pushes SignalR events, but email and SMS delivery are only logged as placeholders, not delegated to Azure Logic Apps. `ReportGenerationService` uses EF Core with `IgnoreQueryFilters()` rather than the Dapper aggregation approach in the design. `ExportService` also falls back to writing plain text bytes that begin with `%PDF-1.4` rather than generating a valid PDF file.

Impact: endpoint surface exists, but the non-trivial behavior described by the design is incomplete or implemented differently.

Evidence:

- `docs/detailed-designs/07-notifications-reporting/README.md:37-39`
- `docs/detailed-designs/07-notifications-reporting/README.md:46-60`
- `docs/detailed-designs/07-notifications-reporting/README.md:150`
- `src/backend/IronvaleFleetHub.Api/Hubs/NotificationHub.cs:16-64`
- `src/backend/IronvaleFleetHub.Api/Controllers/NotificationsController.cs:69-110`
- `src/backend/IronvaleFleetHub.Api/Services/NotificationDispatchService.cs:60-101`
- `src/backend/IronvaleFleetHub.Api/Services/ReportGenerationService.cs:16-76`
- `src/backend/IronvaleFleetHub.Api/Services/ExportService.cs:68-75`

### 10. Alerts are implemented as a minimal workflow and omit threshold management

Feature 05 expects active alerts, acknowledge/resolve actions, and admin or fleet-manager threshold management. The current controller provides list, acknowledge, and resolve only. The list response is a raw `List<Alert>` rather than the paginated contract shown in the design, and there are no threshold-management endpoints.

Impact: alert handling exists but remains a thinner slice than the design.

Evidence:

- `docs/detailed-designs/05-telemetry-monitoring/README.md:40-41`
- `docs/detailed-designs/05-telemetry-monitoring/README.md:54-57`
- `docs/detailed-designs/05-telemetry-monitoring/README.md:146-161`
- `src/backend/IronvaleFleetHub.Api/Controllers/AlertsController.cs:11-81`
- `src/backend/IronvaleFleetHub.Api/Services/AlertEvaluatorService.cs:20-57`

## Medium Findings

### 11. Tenant query filters are only applied to some organization-owned entities

The authentication design positions global query filters as a core tenant-isolation mechanism across tenant-scoped entities. `FleetHubDbContext` currently applies filters only to `Equipment`, `WorkOrder`, `Alert`, `AIPrediction`, `AnomalyDetection`, `Notification`, and `PartsOrder`. It does not apply them to `User`, `UserInvitation`, `NotificationPreference`, `CartItem`, or `TelemetryEvent`.

Impact: the codebase relies more heavily on controller-level filtering than the design suggests, and tenant isolation is not consistently enforced at the EF model level.

Evidence:

- `docs/detailed-designs/01-authentication/README.md:49-50`
- `docs/detailed-designs/01-authentication/README.md:121`
- `src/backend/IronvaleFleetHub.Api/Data/FleetHubDbContext.cs:18-32`
- `src/backend/IronvaleFleetHub.Api/Data/FleetHubDbContext.cs:237-243`

### 12. Security headers are present but not as complete as the design implies

`Program.cs` adds a small set of response headers, but the design set and ADRs consistently assume a stronger production posture around API hardening. The current middleware does not emit HSTS or CSP headers, and there is no broader hardening pipeline around the telemetry or AI endpoints.

Impact: not an immediate correctness issue, but the backend remains below the expected production-hardening baseline implied by the design set.

Evidence:

- `src/backend/IronvaleFleetHub.Api/Program.cs:162-169`

## Design Ambiguities Found During Comparison

### 13. Equipment delete behavior is internally inconsistent in the design

Feature 02 says `DELETE /api/v1/equipment/{id}` is an Admin-only soft delete, but the design decisions section later selects hard delete with cascade delete. The implementation performs hard delete plus related-record cleanup. That matches the later decision, not the earlier API description.

Evidence:

- `docs/detailed-designs/02-equipment-management/README.md:37`
- `docs/detailed-designs/02-equipment-management/README.md:181`
- `src/backend/IronvaleFleetHub.Api/Controllers/EquipmentController.cs:171-203`

### 14. The authentication design mixes single-org and multi-org assumptions

The entity table describes `User` as belonging to one organization, while the open-design decision later resolves the requirement in favor of multiple organizations per user via `UserOrganization`. The implementation currently follows the simpler single-org model.

Evidence:

- `docs/detailed-designs/01-authentication/README.md:86`
- `docs/detailed-designs/01-authentication/README.md:172`
- `src/backend/IronvaleFleetHub.Api/Models/User.cs:6-11`

## Areas That Already Partially Align

- SignalR notification hub exists and supports both organization and user groups.
- Work order numbers already use the documented `WO-YYYY-NNN` shape via `WorkOrderNumberGenerator`.
- Most controllers correctly return `NotFound()` for cross-tenant direct-ID access, which is consistent with the information-leakage guidance in the authentication design.
- Report export formats for PDF, Excel, and CSV are present, even though the implementation details differ from the design.

## Recommended Remediation Order

1. Decide whether Feature 08 is in scope now. If yes, implement the MediatR/CQRS foundation first because many current discrepancies are symptoms of controller-centric architecture.
2. Normalize tenant and identity handling next. Resolve the single-org vs multi-org model, remove header-based tenant override outside dev-only flows, and enforce missing/inactive-org failure behavior.
3. Rework telemetry ingestion and read contracts to match Feature 05 before scaling the data path further.
4. Align frontend-facing API contracts for equipment, parts/orders, AI insights, alerts, and notifications so the SPA can rely on the documented shapes.
5. Replace placeholder integrations. The main examples are email/SMS notification dispatch and the pseudo-PDF fallback.
