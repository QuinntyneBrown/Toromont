# ADR-0004: Minimal APIs Pattern

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Toromont Fleet Hub exposes a RESTful API surface covering equipment management, work orders, telemetry, parts ordering, alerts, reporting, and AI predictions. The project targets .NET 11.0, which provides two primary approaches for defining HTTP endpoints: traditional MVC controllers and the Minimal APIs pattern introduced in .NET 6. The team needs to select an API definition pattern that balances developer productivity, startup performance, and long-term maintainability for a growing API surface.

## Decision

Use the .NET Minimal APIs pattern instead of traditional MVC controllers for defining HTTP endpoints.

## Options Considered

### Option 1: Minimal APIs Pattern (Chosen)
- **Pros:** Reduced boilerplate compared to controllers; faster application startup due to less reflection-based discovery; native AOT compilation compatibility for potential future deployment optimization; simpler endpoint definitions using `app.MapGet`, `app.MapPost`, etc.; full support for the ASP.NET Core middleware pipeline including authentication, authorization, and validation; endpoints can be organized into extension methods for clean separation by domain.
- **Cons:** Less familiar to developers experienced with traditional MVC; endpoint grouping and organization requires deliberate structuring to avoid a monolithic `Program.cs`; some advanced controller features like model binding conventions require explicit configuration.

### Option 2: Traditional MVC Controllers
- **Pros:** Well-established pattern familiar to most .NET developers; built-in conventions for routing, model binding, and filters; attribute-based routing provides clear endpoint definitions; good tooling support in IDEs.
- **Cons:** More ceremony and boilerplate code per endpoint; slower startup due to controller discovery via reflection; controller classes accumulate dependencies and grow large over time; heavier framework overhead for simple request/response scenarios.

### Option 3: Carter Library
- **Pros:** Module-based endpoint organization; familiar to developers from Nancy framework; built-in validation integration.
- **Cons:** Additional third-party dependency; smaller community compared to built-in patterns; adds abstraction over Minimal APIs without substantial benefit for this project's scale; risk of library abandonment or version compatibility issues.

## Consequences

### Positive
- Reduced boilerplate results in faster development velocity for new endpoints.
- Faster startup time benefits containerized deployment and scaling scenarios.
- Native AOT compatibility provides a future optimization path for cold-start performance.
- Direct lambda-based endpoint definitions make request/response flow immediately readable.
- Full middleware pipeline support ensures no loss of cross-cutting concerns like authentication and logging.

### Negative
- Developers accustomed to MVC controllers will need to adapt to the Minimal APIs organization pattern.
- Without deliberate structure, endpoint definitions can become disorganized as the API surface grows.
- The existing empty Controllers folder may cause confusion and should be removed or repurposed.

### Risks
- Endpoint organization may degrade over time if the team does not adopt a consistent grouping convention. Mitigation: define endpoint groups by domain area using static extension methods (e.g., `EquipmentEndpoints`, `WorkOrderEndpoints`).
- Complex endpoint logic inlined in lambda expressions can reduce readability. Mitigation: delegate business logic to injected service classes, keeping endpoint definitions thin.

## Implementation Notes
- Define endpoints using `app.MapGet`, `app.MapPost`, `app.MapPut`, `app.MapDelete` in `Program.cs` or via extension methods.
- Organize endpoints into static classes by domain: `EquipmentEndpoints.cs`, `WorkOrderEndpoints.cs`, `TelemetryEndpoints.cs`, `PartsOrderEndpoints.cs`, `AlertEndpoints.cs`, `ReportEndpoints.cs`, `AIPredictionEndpoints.cs`.
- Use `MapGroup("/api/v1/equipment")` to define route prefixes for each domain area.
- Apply `.RequireAuthorization()` to endpoint groups that require authentication.
- Inject services via endpoint handler parameters using the built-in dependency injection container.
- Remove or repurpose the empty Controllers directory to avoid confusion.

## References
- [L1-011: API Gateway & Integration Design](../design/L1-011-api-gateway-integration.md)
- [Minimal APIs Overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [ADR-0001: ASP.NET Core Backend Framework](0001-aspnet-core-backend-framework.md)
