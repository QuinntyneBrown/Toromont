# ADR-0004: MVC Controller-Based API Pattern

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Ironvale Fleet Hub exposes a RESTful API surface covering equipment management, work orders, telemetry, parts ordering, alerts, reporting, and AI predictions. The project targets .NET 8+, which provides two primary approaches for defining HTTP endpoints: traditional MVC controllers and the Minimal APIs pattern introduced in .NET 6. The team needs to select an API definition pattern that supports the CQRS architecture (via MediatR), attribute-based authorization, and a growing API surface with clear domain separation.

## Decision

Use the traditional ASP.NET Core MVC controller pattern for defining HTTP endpoints. Each feature domain has its own controller class with attribute-based routing, authorization, and clear separation of concerns. Controllers delegate to MediatR handlers for business logic execution.

## Options Considered

### Option 1: Traditional MVC Controllers (Chosen)
- **Pros:** Well-established pattern familiar to most .NET developers; built-in conventions for routing, model binding, and filters; attribute-based routing provides clear endpoint definitions (`[HttpGet]`, `[HttpPost]`, etc.); native support for `[Authorize(Roles = "...")]` and `[Authorize(Policy = "...")]` attributes on controllers and actions; natural grouping of related endpoints by domain (one controller per feature area); excellent tooling support in IDEs; aligns with MediatR CQRS pattern where controllers dispatch commands/queries
- **Cons:** More ceremony and boilerplate per endpoint; controller classes can accumulate dependencies over time; heavier framework overhead for simple request/response scenarios

### Option 2: Minimal APIs Pattern
- **Pros:** Reduced boilerplate; faster startup; native AOT compatibility
- **Cons:** Less familiar organization for enterprise .NET teams; attribute-based authorization requires workarounds; endpoint grouping requires deliberate structuring to avoid monolithic Program.cs; less natural fit with MediatR CQRS pattern

### Option 3: Carter Library
- **Pros:** Module-based endpoint organization; built-in validation integration
- **Cons:** Additional third-party dependency; smaller community; adds abstraction over Minimal APIs without substantial benefit

## Consequences

### Positive
- Clear domain separation with one controller per feature area makes the codebase navigable
- Native `[Authorize(Roles = "...")]` support aligns with the JWT RBAC model (ADR security/0002)
- Controllers act as thin dispatch layers to MediatR handlers, keeping business logic out of the API layer
- Well-understood pattern reduces onboarding time for new team members
- Full support for model binding, filters, and middleware pipeline

### Negative
- More boilerplate per endpoint compared to Minimal APIs
- Controller classes must be kept thin to avoid becoming bloated with business logic
- Slightly slower startup due to controller discovery via reflection

### Risks
- Controller bloat if business logic is not properly delegated to MediatR handlers
- Large controller files if too many endpoints are grouped together

## Implementation Notes
- Controllers organized by domain area:
  - `UsersController` — user management and invitation
  - `EquipmentController` — equipment CRUD and detail views
  - `ImportController` — legacy XML data import
  - `WorkOrdersController` — work order lifecycle and calendar
  - `PartsCatalogController` — parts browsing and search
  - `CartController` — shopping cart operations
  - `PartsOrdersController` — order submission and history
  - `TelemetryController` — telemetry data queries
  - `AlertsController` — alert viewing and acknowledgment
  - `AIInsightsController` — predictions and anomalies
  - `NotificationsController` — notification listing and preferences
  - `ReportsController` — report generation and export
- All controllers inherit from `ControllerBase` and use `[ApiController]` attribute
- Route convention: `[Route("api/v1/[controller]")]`
- Controllers dispatch commands and queries via `IMediator.Send()`
- Apply `[Authorize(Policy = "RequireFleetManager")]` on controllers/actions as needed

## References
- [L1-011: API Gateway & Integration Design](../design/L1-011-api-gateway-integration.md)
- [ASP.NET Core Controllers](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [ADR-0001: ASP.NET Core Backend Framework](0001-aspnet-core-backend-framework.md)
- [ADR backend/0008: MediatR CQRS Pattern](0008-mediatr-cqrs-pattern.md)
