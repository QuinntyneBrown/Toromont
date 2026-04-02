# ADR-0006: RESTful API with URL Path Versioning

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Ironvale Fleet Hub exposes APIs consumed by the Angular frontend application and potentially by third-party integrations in the future. The API surface covers equipment CRUD, work order management, telemetry ingestion, parts ordering, alert configuration, reporting, and AI predictions. The team needs a consistent API design strategy that supports evolution without breaking existing clients, provides clear documentation, and follows industry-standard conventions. A versioning strategy is required to allow non-breaking API evolution while maintaining backward compatibility during transition periods.

## Decision

Use RESTful API design with URL path versioning (`/api/v1/`) and consistent JSON response envelopes including a `PaginatedResponse<T>` wrapper for collection endpoints.

## Options Considered

### Option 1: RESTful API with URL Path Versioning (Chosen)
- **Pros:** Industry-standard design pattern with excellent tooling support; URL path versioning (`/api/v1/`) is the most explicit and discoverable versioning approach; URLs are independently cacheable by version; OpenAPI/Swagger generates comprehensive interactive documentation automatically; simple for frontend and third-party clients to consume; well-understood HTTP semantics (GET, POST, PUT, DELETE) map naturally to fleet management operations.
- **Cons:** URL path versioning requires maintaining separate route registrations per version; RESTful design can require multiple round-trips for complex operations; resource-oriented design may not fit all use cases naturally (e.g., triggering a report generation).

### Option 2: GraphQL
- **Pros:** Flexible client-driven queries; single endpoint reduces over-fetching; strong type system with introspection.
- **Cons:** Higher complexity for the backend implementation; caching is significantly more complex than REST; less mature tooling in the .NET ecosystem compared to REST; overkill for the predominantly CRUD-oriented fleet management domain; steeper learning curve for frontend developers.

### Option 3: gRPC
- **Pros:** Excellent performance with binary serialization; strongly typed contracts via protobuf; bidirectional streaming support.
- **Cons:** Poor browser support without gRPC-Web proxy; not human-readable for debugging; limited tooling for API exploration and documentation; less suitable for public-facing APIs consumed by web frontends.

### Option 4: Query String Versioning
- **Pros:** Single URL path per resource; version is an optional parameter.
- **Cons:** Less discoverable than URL path versioning; complicates caching since the same path serves different versions; easy to omit the version parameter leading to ambiguous behavior.

### Option 5: Header Versioning
- **Pros:** Clean URLs without version segments; separation of concerns between resource identity and version.
- **Cons:** Not visible in URLs making debugging and documentation harder; requires custom header configuration on every client; poor discoverability; complicates API exploration in browsers.

## Consequences

### Positive
- Explicit URL versioning makes API version immediately visible in logs, documentation, and client code.
- OpenAPI/Swagger provides interactive documentation that accelerates frontend development and third-party onboarding.
- Consistent response envelopes (`PaginatedResponse<T>`) simplify client-side data handling and pagination logic.
- Standard HTTP status codes (201 Created, 400 Bad Request, 403 Forbidden, 404 Not Found, 429 Too Many Requests) provide clear, predictable error semantics.
- URL-based versions are independently cacheable, improving performance for CDN and proxy scenarios.

### Negative
- Multiple API versions increase maintenance burden when both must be supported simultaneously.
- RESTful design may require multiple endpoints for complex workflows that span several resources.
- URL path versioning makes URLs slightly longer and requires route group configuration per version.

### Risks
- Without governance, API design inconsistencies may emerge across different domain areas. Mitigation: establish API design guidelines and review new endpoints for consistency.
- Version proliferation without a deprecation policy could lead to indefinite maintenance of old versions. Mitigation: define a version deprecation policy with a minimum 6-month sunset period.

## Implementation Notes
- All API routes use the `/api/v1/` prefix, e.g., `/api/v1/equipment`, `/api/v1/work-orders`, `/api/v1/telemetry`.
- Collection endpoints accept pagination query parameters: `page` (default 1), `pageSize` (default 20, max 100), `sortBy`, `sortDir` (asc/desc).
- Collection responses use `PaginatedResponse<T>` envelope containing `items`, `totalCount`, `page`, `pageSize`, and `totalPages`.
- Error responses use a consistent `ApiResponse` envelope with `message` and optional `errors` array for validation failures.
- Standard HTTP status codes:
  - `201 Created` for successful resource creation with `Location` header.
  - `400 Bad Request` for validation errors with detailed error messages.
  - `403 Forbidden` for authorization failures.
  - `404 Not Found` for missing resources.
  - `429 Too Many Requests` for rate limit violations with `Retry-After` header.
- Enable Swagger UI in Development and Staging environments for interactive API exploration.

## References
- [L1-011: API Gateway & Integration Design](../design/L1-011-api-gateway-integration.md)
- [ADR-0004: Minimal APIs Pattern](0004-minimal-apis-pattern.md)
- [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines)
- [OpenAPI Specification](https://spec.openapis.org/oas/latest.html)
