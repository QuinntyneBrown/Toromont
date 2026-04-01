# ADR-0001: ASP.NET Core Backend Framework

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Toromont Fleet Hub requires a high-performance, enterprise-grade web framework to power fleet management APIs. The backend must serve equipment telemetry ingestion, work order lifecycle management, parts ordering workflows, and AI-driven predictive insights. The chosen framework must support robust authentication, real-time data processing, and seamless integration with Azure cloud services. Given the scale of fleet operations and the criticality of uptime, the framework must deliver strong performance characteristics and a mature ecosystem for long-term maintainability.

## Decision

Use ASP.NET Core on .NET 8+ as the backend framework for Toromont Fleet Hub.

## Options Considered

### Option 1: ASP.NET Core on .NET 8+ (Chosen)
- **Pros:** C# type safety reduces runtime errors across complex domain models; mature enterprise ecosystem with extensive NuGet package library; excellent Azure integration for deployment, monitoring, and managed services; built-in dependency injection container; first-class OpenAPI/Swagger support for API documentation; strong performance benchmarks for API workloads; long-term support and active development by Microsoft.
- **Cons:** Smaller talent pool compared to Node.js; heavier runtime footprint than Go; steeper learning curve for developers unfamiliar with .NET.

### Option 2: Node.js with Express
- **Pros:** Large developer community; lightweight and fast for I/O-bound workloads; JavaScript/TypeScript shared with frontend.
- **Cons:** Lack of strong typing without TypeScript discipline; callback/async complexity at scale; weaker enterprise library ecosystem for complex domain logic; single-threaded event loop can bottleneck CPU-bound operations like report generation.

### Option 3: Java Spring Boot
- **Pros:** Proven enterprise framework; strong typing with Java; extensive ecosystem and tooling.
- **Cons:** Verbose boilerplate code; slower startup times; heavier memory consumption; less natural Azure integration compared to .NET.

### Option 4: Go
- **Pros:** Excellent raw performance; lightweight binaries; strong concurrency primitives.
- **Cons:** Less mature web framework ecosystem; limited ORM options; more manual work for enterprise patterns like dependency injection and middleware pipelines; smaller library ecosystem for reporting and document generation.

## Consequences

### Positive
- Strong type safety with C# reduces defects in domain models spanning 13+ entities.
- Native Azure integration simplifies deployment, monitoring, and scaling.
- Built-in dependency injection supports clean architecture and testability.
- OpenAPI support enables automatic API documentation for frontend and third-party consumers.
- High throughput for API workloads supports telemetry ingestion and real-time fleet monitoring.

### Negative
- Team members without .NET experience will require onboarding and training.
- .NET runtime has a larger deployment footprint than Go or Node.js.
- Tighter coupling to the Microsoft ecosystem may limit future cloud portability.

### Risks
- .NET 8+ is a Long Term Support release; must plan upgrades as new LTS versions are released.
- Over-reliance on Microsoft-specific patterns could make future platform migration costly.

## Implementation Notes
- Target .NET 8+ with the `net8.0 or later` target framework moniker.
- Use the `WebApplication.CreateBuilder` pattern for application bootstrapping.
- Configure middleware pipeline for authentication, CORS, error handling, and request logging.
- Leverage built-in `IServiceCollection` for dependency injection across all service layers.
- Enable OpenAPI generation for all public API endpoints.

## References
- [L1-011: API Gateway & Integration Design](../design/L1-011-api-gateway-integration.md)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [.NET 8+ Release Notes](https://learn.microsoft.com/en-us/dotnet/)
