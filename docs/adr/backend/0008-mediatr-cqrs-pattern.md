# ADR-0008: MediatR CQRS Pattern

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

All 7 detailed designs specify MediatR as a core architectural component. Controllers dispatch commands and queries to MediatR handlers rather than containing business logic directly. This separates the API layer from business logic and enables cross-cutting concerns via pipeline behaviors. The application needs a consistent pattern for handling commands (writes) and queries (reads) across all feature modules while keeping controllers thin and focused on HTTP concerns.

## Decision

Use MediatR library to implement CQRS (Command Query Responsibility Segregation) pattern across all API features.

## Options Considered

### Option 1: MediatR CQRS (Chosen)
- **Pros:** Clean separation of command/query handling from API controllers; pipeline behaviors for validation, logging, and transaction management; single responsibility per handler; each handler is testable in isolation; well-established .NET library with large community and active maintenance; integrates with FluentValidation via pipeline behaviors for automatic request validation.
- **Cons:** Additional abstraction layer adds indirection; developers must learn MediatR conventions; simple CRUD operations may feel over-engineered with dedicated handlers; debugging requires tracing through the mediator pipeline.

### Option 2: Direct Service Injection
- **Pros:** Simpler architecture with fewer abstractions; direct method calls are easy to trace and debug; no library dependency required.
- **Cons:** Controllers become fat with business logic or require manual service orchestration; cross-cutting concerns like validation and logging must be applied manually per endpoint; harder to enforce single responsibility principle; tighter coupling between API layer and business logic.

### Option 3: Custom Mediator Implementation
- **Pros:** Full control over the mediator pipeline; no external library dependency; can be tailored exactly to application needs.
- **Cons:** Significant development and maintenance effort; lacks the community support, documentation, and battle-testing of MediatR; reinvents the wheel for a well-solved problem; pipeline behavior infrastructure must be built from scratch.

### Option 4: Wolverine
- **Pros:** Modern .NET messaging framework with built-in middleware pipeline; supports both in-process and distributed messaging; less boilerplate than MediatR for simple cases.
- **Cons:** Smaller community and ecosystem than MediatR; more opinionated framework that may conflict with other architectural choices; less established in enterprise .NET applications; steeper learning curve for the team.

## Consequences

### Positive
- Controllers remain thin dispatch layers, calling only `IMediator.Send()` and returning results.
- Each command or query has a dedicated handler with a single responsibility, making the codebase easier to navigate and maintain.
- Pipeline behaviors provide a centralized mechanism for cross-cutting concerns: FluentValidation runs automatically before handlers, logging captures all requests and responses, and transactions can be managed declaratively.
- Handlers are testable in isolation without requiring HTTP infrastructure or controller setup.
- New features follow a consistent pattern: define a request, create a handler, register validation rules.

### Negative
- Additional indirection makes it harder to trace the flow from controller to business logic without familiarity with MediatR conventions.
- Simple CRUD operations require more files (request class, handler class, validator class) than direct service injection.
- Developers new to MediatR need onboarding time to understand the request/handler/pipeline model.

### Risks
- Over-reliance on pipeline behaviors could obscure important logic that would be more visible in explicit code. Mitigation: reserve pipeline behaviors for truly cross-cutting concerns only.
- Handler proliferation may make the project structure harder to navigate as the number of features grows. Mitigation: organize handlers by feature area using a vertical slice folder structure.
- MediatR library updates or breaking changes could require refactoring across all handlers. Mitigation: the library has a stable API and follows semantic versioning.

## Implementation Notes

- Each feature area has dedicated command/query handlers:
  - **Auth:** `InviteUserHandler`, `ChangeUserRoleHandler`
  - **Equipment:** `CreateEquipmentHandler`, `UpdateEquipmentHandler`, `ImportEquipmentHandler`
  - **Work Orders:** `CreateWorkOrderHandler`, `UpdateWorkOrderStatusHandler`, `GetCalendarDataHandler`
  - **Parts:** `SearchPartsHandler`, `SubmitOrderHandler`
  - **Telemetry:** `TelemetryController` dispatches via MediatR
  - **AI:** `AIInsightsController` dispatches via MediatR
  - **Reports/Notifications:** controllers dispatch via MediatR
- Controllers call `IMediator.Send()` and remain thin dispatch layers that handle only HTTP concerns (status codes, content negotiation).
- FluentValidation integrates via MediatR pipeline behavior (`ValidationBehavior<TRequest, TResponse>`) for automatic request validation before handler execution.
- Register MediatR in `Program.cs` with `builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly))`.
- Pipeline behavior registration order: Logging → Validation → Transaction → Handler.

## References

- [L1-011: System Architecture Requirements](../design/L1-011-system-architecture.md)
- [MediatR GitHub Repository](https://github.com/jbogard/MediatR)
- [ADR-0009: FluentValidation Server-Side Validation](0009-fluentvalidation-server-side-validation.md)
