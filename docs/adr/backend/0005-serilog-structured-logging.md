# ADR-0005: Serilog Structured Logging

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Ironvale Fleet Hub requires comprehensive observability across fleet management operations. The system must log equipment alert triggers, work order state transitions, telemetry processing throughput, authentication and authorization events, parts order lifecycle changes, and AI prediction requests. Logs must support correlation across distributed operations, structured querying in Azure Application Insights, and configurable verbosity levels to manage log volume in production. The built-in `ILogger` abstraction provides a foundation, but a more capable logging framework is needed for structured properties, enrichment, and flexible sink configuration.

## Decision

Use Serilog for structured logging with Azure Application Insights as the primary sink.

## Options Considered

### Option 1: Serilog with Application Insights Sink (Chosen)
- **Pros:** Rich structured logging with typed properties that are queryable in Application Insights; correlation ID enrichment for tracing requests across service boundaries; multiple sink support (console for development, Application Insights for production); configurable log levels per namespace (e.g., Information for application code, Warning for ASP.NET Core internals); seamless integration with ASP.NET Core's `ILogger` abstraction via `UseSerilog()`; request logging middleware with automatic HTTP context enrichment.
- **Cons:** Additional NuGet dependency; Serilog configuration adds complexity to application startup; developers must learn structured logging patterns and property naming conventions.

### Option 2: Built-in ILogger Only
- **Pros:** Zero additional dependencies; built-in to ASP.NET Core; simple configuration via `appsettings.json`.
- **Cons:** Limited structured logging capabilities; no built-in enrichment for correlation IDs or tenant context; fewer sink options; less flexible filtering configuration; no request logging middleware.

### Option 3: NLog
- **Pros:** Mature logging framework; extensive target (sink) ecosystem; XML-based configuration.
- **Cons:** XML configuration is more verbose and harder to manage than Serilog's fluent API; less natural fit with modern .NET dependency injection patterns; structured logging support is less ergonomic than Serilog.

### Option 4: log4net
- **Pros:** Long-established framework; familiar to developers from Java ecosystem.
- **Cons:** Declining adoption in .NET ecosystem; weaker structured logging support; XML-heavy configuration; less active development compared to Serilog.

## Consequences

### Positive
- Structured log properties enable precise querying in Application Insights (e.g., filter by EquipmentId, WorkOrderId, OrganizationId).
- Correlation IDs trace requests end-to-end from API entry through telemetry processing and database operations.
- Per-namespace log level configuration keeps production log volume manageable while retaining diagnostic detail for application code.
- Request logging middleware provides automatic HTTP method, path, status code, and duration logging.
- Console sink during development provides immediate, readable feedback.

### Negative
- Serilog adds a third-party dependency that must be kept up to date.
- Developers unfamiliar with structured logging may fall back to string interpolation, losing queryability.
- Application Insights sink configuration requires proper instrumentation key management.

### Risks
- Excessive logging in high-throughput telemetry paths could impact performance and increase Application Insights costs. Mitigation: use Warning or Error level for telemetry processing internals; log aggregate metrics rather than individual events.
- Sensitive data (e.g., user tokens, personal information) could be inadvertently logged in structured properties. Mitigation: establish a logging policy that prohibits PII in log properties and use Serilog's destructuring policies to mask sensitive fields.

## Implementation Notes
- Configure Serilog in `Program.cs` using `builder.Host.UseSerilog()`.
- Set default minimum log level to `Information` for application namespaces.
- Set `Microsoft.AspNetCore` namespace to `Warning` to reduce framework noise.
- Enable `app.UseSerilogRequestLogging()` middleware for automatic HTTP request logging.
- Enrich all log entries with `CorrelationId`, `OrganizationId`, and `UserId` from the current request context.
- Configure sinks: `Console` for Development environment, `ApplicationInsights` for Staging and Production.
- Use structured logging syntax: `Log.Information("Work order {WorkOrderId} transitioned to {Status}", workOrderId, newStatus)` rather than string interpolation.

## References
- [L1-012: Observability & Reliability Design](../design/L1-012-observability-reliability.md)
- [Serilog Documentation](https://serilog.net/)
- [Serilog.Sinks.ApplicationInsights](https://github.com/serilog-contrib/serilog-sinks-applicationinsights)
