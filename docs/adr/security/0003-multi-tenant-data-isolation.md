# ADR-0003: Multi-Tenant Data Isolation with EF Core Global Query Filters

**Date:** 2026-04-01
**Category:** security
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Ironvale Fleet Hub is a multi-tenant application serving multiple organizations (Ironvale customers) from a single deployed instance. Each organization's data -- equipment records, work orders, users, parts inventory, telemetry events, alerts, and more -- must be strictly isolated so that no organization can view, modify, or infer the existence of another organization's data. The isolation mechanism must be reliable, difficult to bypass accidentally, and operationally manageable as the number of tenants grows.

## Decision

Use a single shared database with tenant isolation enforced through Entity Framework Core global query filters on `OrganizationId`. All major domain entities include an `OrganizationId` foreign key column. A `TenantContextMiddleware` component extracts the `org_id` claim from the authenticated user's JWT on every request and sets the tenant context for the current scope. EF Core's global query filters automatically append `WHERE OrganizationId = @tenantId` to all LINQ queries against tenant-scoped entities. When a user attempts to access a resource belonging to a different organization, the system returns HTTP 404 (not 403) to prevent information leakage about the existence of other tenants' data. Database indexes are scoped to `OrganizationId` to ensure query performance and enforce uniqueness within a tenant boundary.

## Options Considered

### Option 1: Single Database with EF Core Global Query Filters (chosen)

- **Pros:**
  - Operationally simple: one database, one connection string, one migration path across all tenants
  - Global query filters are applied automatically by EF Core, reducing the risk of developers accidentally writing unfiltered queries
  - Straightforward to add new tenants without provisioning infrastructure
  - Single database simplifies backup, restore, and disaster recovery procedures
  - Cross-tenant reporting for Ironvale internal use (e.g., aggregate fleet analytics) is possible with administrative bypass queries

- **Cons:**
  - A bug in the filter configuration or a raw SQL query that bypasses EF Core could leak data across tenants
  - All tenants share database resources; a heavy-usage tenant could affect performance for others (noisy neighbor)
  - Tenant data deletion or offboarding requires targeted data removal rather than simply dropping a database

### Option 2: Database-Per-Tenant

- **Pros:**
  - Strongest isolation guarantee: tenants are physically separated at the database level
  - Easy to offboard a tenant by removing their database
  - No risk of cross-tenant query leakage

- **Cons:**
  - Significant operational overhead: each new tenant requires provisioning a new database
  - Schema migrations must be applied independently to every tenant database
  - Connection management and pooling become complex as tenant count grows
  - Higher infrastructure cost for many small tenants

### Option 3: Schema-Per-Tenant

- **Pros:**
  - Logical separation within a single database instance
  - Moderate isolation without the full cost of database-per-tenant

- **Cons:**
  - EF Core has limited native support for dynamic schema switching at runtime
  - Migration management across multiple schemas adds complexity
  - Uncommon pattern in the .NET ecosystem, leading to less community support and tooling

### Option 4: Application-Level Filtering (manual WHERE clauses)

- **Pros:**
  - No framework-level configuration required; developers add filters explicitly
  - Maximum flexibility in how and where filtering is applied

- **Cons:**
  - Extremely error-prone: every query must remember to include the tenant filter
  - A single missed filter clause creates a cross-tenant data leakage vulnerability
  - Difficult to audit and enforce consistently across a growing codebase
  - No compile-time or framework-level safety net

## Consequences

### Positive

- Tenant isolation is enforced at the data access layer automatically, reducing the surface area for developer error
- Single database architecture keeps operational complexity low and allows the team to focus on application features rather than infrastructure management
- The 404-not-403 pattern prevents reconnaissance attacks where an adversary could enumerate resources in other tenants by distinguishing "forbidden" from "not found" responses
- Scoped unique indexes (e.g., `IX_Equipment_OrganizationId_SerialNumber`) enforce data integrity within each tenant while allowing the same serial number to exist in different organizations
- New tenant onboarding is a data operation (insert organization record, assign users) rather than an infrastructure operation

### Negative

- Raw SQL queries, stored procedures, or any data access that bypasses EF Core's query pipeline will not have global filters applied and must be manually secured
- The noisy neighbor problem is not addressed by this architecture alone; additional measures (rate limiting, resource quotas) may be needed as tenant count and data volume grow
- Tenant offboarding requires careful, targeted deletion of all related records across multiple tables rather than a simple database drop

### Risks

- A misconfigured or accidentally disabled global query filter on any entity could silently expose cross-tenant data; this must be covered by integration tests
- If the `TenantContextMiddleware` fails to set the tenant context (e.g., missing `org_id` claim), queries could either fail or return unfiltered results depending on the fallback behavior; the middleware must reject requests with no tenant context
- As data volume grows, single-database performance may degrade; partitioning strategies or read replicas may be needed in the future
- Developers writing `IgnoreQueryFilters()` for legitimate administrative scenarios must be carefully reviewed to prevent accidental exposure in user-facing endpoints

## Implementation Notes

- `TenantContextMiddleware` runs early in the ASP.NET Core pipeline, extracts `org_id` from the JWT claims, and stores it in a scoped `ITenantContext` service
- The `DbContext` configures global query filters in `OnModelCreating` for all tenant-scoped entities:
  ```csharp
  modelBuilder.Entity<Equipment>().HasQueryFilter(e => e.OrganizationId == _tenantContext.OrganizationId);
  ```
- Tenant-scoped entities include: Equipment, WorkOrder, User, Part, Alert, TelemetryEvent, Notification, AIPrediction, AnomalyDetection, PartsOrder, and OrderLineItem
- Database indexes scoped to OrganizationId:
  - `IX_Equipment_OrganizationId_SerialNumber` (unique)
  - `IX_WorkOrder_OrganizationId_Status`
  - `IX_User_OrganizationId_Email` (unique)
  - `IX_TelemetryEvent_OrganizationId_Timestamp`
- All API endpoints that access tenant-scoped data must verify that the requested resource's OrganizationId matches the current tenant context; mismatches return 404
- Requests without a valid `org_id` claim are rejected with 401 before reaching any controller logic
- Integration tests must verify that a user in Organization A cannot retrieve, modify, or detect resources belonging to Organization B
- Administrative endpoints that require cross-tenant access (e.g., platform-level reporting) must use explicitly documented `IgnoreQueryFilters()` calls and be restricted to Admin role with additional audit logging

## References

- L1-007: Core Equipment & Fleet Management
- L1-013: Security & Compliance
- ADR-0001: Microsoft Entra ID Authentication with OAuth2 Authorization Code Flow + PKCE
- ADR-0002: JWT Claims-Based Role-Based Access Control
- [EF Core Global Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- [Multi-Tenancy in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/multi-tenant)
