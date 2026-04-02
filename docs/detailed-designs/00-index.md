# Detailed Designs — Index

## Ironvale Fleet Hub — Equipment Fleet Management & Service Portal

| # | Feature | Status | Description |
|---|---------|--------|-------------|
| 01 | [Authentication & Multi-Tenancy](01-authentication/README.md) | Draft | Entra ID auth, OAuth2/OIDC, RBAC, claims-based multi-tenant isolation |
| 02 | [Equipment Management](02-equipment-management/README.md) | Draft | Equipment registry CRUD, detail views, legacy XML import |
| 03 | [Service Management](03-service-management/README.md) | Draft | Work orders, lifecycle tracking, scheduling calendar |
| 04 | [Parts & Ordering](04-parts-ordering/README.md) | Draft | Parts catalog, AI search, shopping cart, order submission |
| 05 | [Telemetry & Monitoring](05-telemetry-monitoring/README.md) | Draft | Event ingestion, real-time charts, Azure Functions processing |
| 06 | [AI Insights](06-ai-insights/README.md) | Draft | Predictive maintenance, anomaly detection, ML pipeline |
| 07 | [Notifications & Reporting](07-notifications-reporting/README.md) | Draft | SignalR in-app, email/SMS via Logic Apps, fleet reports, exports |
| 08 | [MediatR CQRS Refactor](08-mediatr-cqrs-refactor/README.md) | Draft | Move controller logic into MediatR request handlers with pipeline behaviors |
| 09 | [Tenant & Identity Hardening](09-tenant-identity-hardening/README.md) | Draft | Multi-org membership, tenant enforcement, deactivation blacklist, accept-invite (Audit #2, #4, #11) |
| 10 | [Telemetry Ingestion Redesign](10-telemetry-ingestion-redesign/README.md) | Draft | Azure Function ingestion, Dapper bulk inserts, retry/dead-letter, alert pagination/thresholds (Audit #3, #10) |
| 11 | [Notification Wiring](11-notification-wiring/README.md) | Draft | Wire notification bell/dropdown, REST mark-as-read, SignalR init, preferences API, deep-link fixes (Frontend Audit #1) |
| 12 | [Service Management Redesign](12-service-management-redesign/README.md) | Draft | Kendo Grid with server-side ops, Kendo Scheduler calendar, history from detail endpoint, no optimistic updates (Frontend Audit #2) |
| 13 | [Work Order Identity Fix](13-work-order-identity-fix/README.md) | Draft | Replace hardcoded technician list with API user lookup, send valid GUID as assignedToUserId (Frontend Audit #3) |
