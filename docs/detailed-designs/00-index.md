# Detailed Designs — Index

## Toromont Fleet Hub — Equipment Fleet Management & Service Portal

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
