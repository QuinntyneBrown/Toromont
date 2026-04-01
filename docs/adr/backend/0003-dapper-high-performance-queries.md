# ADR-0003: Dapper for High-Performance Queries

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Toromont Fleet Hub processes equipment telemetry data at rates exceeding 100 events per second and must generate fleet utilization and maintenance cost reports within a 3-second response target. Entity Framework Core, while excellent for general CRUD operations (see ADR-0002), introduces unacceptable overhead for these specific workloads. Telemetry bulk inserts through EF Core's change tracker create significant memory pressure and latency. Complex reporting queries requiring SQL window functions, CTEs, and multi-table aggregations produce suboptimal SQL when expressed through LINQ.

## Decision

Use Dapper alongside Entity Framework Core for telemetry ingestion (100+ events/sec bulk inserts) and reporting queries targeting sub-3-second response times.

## Options Considered

### Option 1: Dapper alongside EF Core (Chosen)
- **Pros:** Near-raw SQL performance with minimal overhead; simple object mapping without change tracking burden; works alongside EF Core using the same database connection; supports bulk insert operations efficiently; full SQL control for window functions and CTEs needed by reporting queries; well-maintained and widely adopted in the .NET ecosystem.
- **Cons:** Two data access patterns to maintain; raw SQL strings lack compile-time validation; developers must manually apply OrganizationId tenant filtering in Dapper queries; no migration support (handled by EF Core).

### Option 2: EF Core for Everything
- **Pros:** Single data access pattern; consistent tenant filtering via global query filters; compile-time LINQ validation.
- **Cons:** Change tracker overhead unacceptable for 100+ events/sec telemetry ingestion; LINQ cannot express SQL window functions needed for utilization reports; bulk insert performance falls well short of requirements; reporting queries exceed the 3-second target.

### Option 3: Raw ADO.NET
- **Pros:** Maximum possible performance; zero abstraction overhead.
- **Cons:** Extremely verbose; manual parameter binding and result mapping; no meaningful advantage over Dapper for the marginal performance difference; higher maintenance cost.

### Option 4: Stored Procedures Only
- **Pros:** Database-level optimization; execution plan caching.
- **Cons:** Business logic split between application and database layers; harder to version control and deploy; reduces developer productivity; complicates unit testing.

## Consequences

### Positive
- Telemetry bulk inserts achieve the required throughput of 100+ events per second.
- Reporting queries leverage SQL window functions and CTEs for fleet utilization and maintenance cost calculations within the 3-second target.
- Dapper's minimal overhead keeps memory consumption stable during high-throughput telemetry processing.
- Full SQL control allows query optimization for specific database engine capabilities.

### Negative
- Two data access patterns increase cognitive load for developers working across the codebase.
- Raw SQL strings in Dapper queries are not validated at compile time, increasing the risk of runtime SQL errors.
- Tenant isolation must be manually enforced in every Dapper query since global query filters do not apply.

### Risks
- Developers may forget to include OrganizationId filtering in Dapper queries, causing cross-tenant data exposure. Mitigation: create helper methods that automatically inject tenant parameters and enforce their use via code review.
- SQL strings may drift out of sync with schema changes made via EF Core migrations. Mitigation: integration tests that exercise all Dapper queries against the current schema.
- Over-use of Dapper for scenarios where EF Core is sufficient could fragment the codebase. Mitigation: establish clear guidelines that Dapper is reserved for telemetry ingestion and reporting only.

## Implementation Notes
- Use Dapper for `TelemetryEvent` bulk inserts via parameterized `INSERT` statements with batching.
- Use Dapper for fleet utilization reports with SQL window functions (`ROW_NUMBER`, `LAG`, `LEAD`, `SUM OVER`).
- Use Dapper for maintenance cost reports requiring multi-table joins with aggregation and grouping.
- Create a `DapperQueryService` or equivalent that accepts the current tenant context and automatically injects OrganizationId parameters.
- Share the same connection string and database as EF Core; obtain `DbConnection` from `ToromontFleetHubDbContext` when needed.
- All Dapper SQL queries must be covered by integration tests that validate correct results and tenant isolation.

## References
- [L1-005: Telemetry Ingestion Pipeline](../design/L1-005-telemetry-ingestion.md)
- [L1-009: Reporting & Analytics](../design/L1-009-reporting-analytics.md)
- [ADR-0002: Entity Framework Core ORM](0002-entity-framework-core-orm.md)
- [Dapper GitHub Repository](https://github.com/DapperLib/Dapper)
