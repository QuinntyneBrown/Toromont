# ADR-0002: Entity Framework Core ORM

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Ironvale Fleet Hub requires a data access layer that supports 13+ domain entities with complex relationships, including Equipment, WorkOrder, TelemetryEvent, PartsOrder, User, Alert, and others. The system operates in a multi-tenant environment where data isolation by OrganizationId is a critical security requirement. The ORM must support code-first schema evolution, complex querying with joins and aggregations, and automatic tenant scoping to prevent cross-organization data leakage.

## Decision

Use Entity Framework Core as the primary ORM with global query filters for multi-tenant isolation via OrganizationId.

## Options Considered

### Option 1: Entity Framework Core with Global Query Filters (Chosen)
- **Pros:** Code-first migrations enable version-controlled schema evolution; LINQ queries provide compile-time query validation; global query filters automatically scope all queries to the current tenant's OrganizationId; navigation properties simplify traversal of entity relationships; change tracking reduces boilerplate for update operations; strong integration with ASP.NET Core dependency injection.
- **Cons:** Performance overhead for high-volume bulk operations; generated SQL can be suboptimal for complex reporting queries; change tracker memory pressure under heavy load; learning curve for advanced features like owned types and value converters.

### Option 2: Dapper Only
- **Pros:** Near-raw SQL performance; full control over generated queries; minimal abstraction overhead.
- **Cons:** No built-in migration tooling; manual mapping of all entity relationships; no automatic tenant scoping requiring manual WHERE clauses on every query; no change tracking; significantly more boilerplate code for CRUD operations across 13+ entities.

### Option 3: NHibernate
- **Pros:** Mature ORM with advanced mapping capabilities; supports complex inheritance strategies.
- **Cons:** Declining community adoption in .NET ecosystem; heavier configuration overhead; less natural integration with modern .NET patterns; smaller pool of developers with experience.

### Option 4: Raw ADO.NET
- **Pros:** Maximum performance; zero abstraction overhead; full SQL control.
- **Cons:** Extremely verbose for CRUD operations; no migration support; manual connection management; no compile-time query validation; tenant isolation must be manually enforced on every query; highest maintenance burden.

## Consequences

### Positive
- Global query filters guarantee tenant isolation at the data access layer, reducing the risk of cross-organization data leaks.
- Code-first migrations provide repeatable, version-controlled database schema deployments.
- LINQ queries catch query errors at compile time rather than runtime.
- Navigation properties simplify loading of related entities such as WorkOrder with its Equipment and assigned Technician.
- Change tracking streamlines update operations for complex entity graphs.

### Negative
- EF Core overhead is unacceptable for bulk telemetry ingestion and complex reporting queries (mitigated by Dapper, see ADR-0003).
- Generated SQL requires monitoring to catch N+1 query patterns and inefficient joins.
- Global query filters add a small overhead to every query execution.

### Risks
- Developers may bypass global query filters using `IgnoreQueryFilters()`, inadvertently exposing cross-tenant data. Code reviews must enforce this.
- EF Core migrations can become complex with branching; a migration strategy and naming convention must be established.
- Change tracker memory pressure during batch operations could cause performance degradation if not properly managed with `AsNoTracking()`.

## Implementation Notes
- Configure `IronvaleFleetHubDbContext` with global query filters applying `OrganizationId` scoping to all tenant-specific entities.
- Use code-first migrations stored in the `Data/Migrations` directory.
- Apply `AsNoTracking()` for read-only query scenarios to reduce memory overhead.
- Use explicit loading or projection via `.Select()` instead of eager loading with `.Include()` where possible to minimize data transfer.
- Dapper is used alongside EF Core for performance-critical paths (see ADR-0003).

## References
- [L1-002: Data Architecture Design](../design/L1-002-data-architecture.md)
- [L1-007: Multi-Tenant Data Isolation](../design/L1-007-multi-tenant-isolation.md)
- [ADR-0003: Dapper for High-Performance Queries](0003-dapper-high-performance-queries.md)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
