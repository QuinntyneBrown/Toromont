# ADR-0001: SQL Server 2022 as Primary Relational Database

**Date:** 2026-04-01
**Category:** data
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

The Toromont Fleet Hub fleet management system requires a robust relational database to support 13+ entity types with complex relationships, multi-tenant data isolation by organization, time-series telemetry ingestion from heavy equipment, and reporting workloads that leverage window functions. The system is built on the Microsoft ecosystem (ASP.NET Core, EF Core, Azure) and must scale to handle growing fleets across multiple Toromont divisions. Currently, the prototype uses EF Core InMemory provider for rapid development and will migrate to a production-grade database engine.

## Decision

Use SQL Server 2022 as the primary relational database for the Toromont Fleet Hub, with Azure SQL Database as the managed deployment option.

## Options Considered

### Option 1: SQL Server 2022 (chosen)

- **Pros:**
  - Enterprise licensing alignment with Toromont's existing Microsoft ecosystem and Azure investment
  - Excellent EF Core provider support with mature tooling and first-class integration
  - Window functions (ROW_NUMBER, RANK, LAG, LEAD) enable complex fleet reporting and trend analysis
  - Full-text search capabilities for the parts catalog, enabling natural-language search over part descriptions and specifications
  - Table partitioning for telemetry data allows efficient date-range queries and data lifecycle management
  - Azure SQL Database managed option reduces operational overhead with built-in high availability, automated backups, and intelligent performance tuning
  - Row-level security support for multi-tenant data isolation by OrganizationId
  - Temporal tables for audit trails on critical entities such as equipment and maintenance records
- **Cons:**
  - Higher licensing cost compared to open-source alternatives
  - Vendor lock-in to the Microsoft ecosystem
  - Less community-driven innovation compared to PostgreSQL

### Option 2: PostgreSQL

- **Pros:**
  - Open-source with no licensing costs
  - Strong community and extension ecosystem (TimescaleDB for time-series)
  - Excellent JSON support for flexible schemas
- **Cons:**
  - Less alignment with existing Microsoft tooling and team expertise
  - EF Core provider is mature but not as tightly integrated as the SQL Server provider
  - Would require separate operational expertise from the rest of the Microsoft stack

### Option 3: Azure Cosmos DB

- **Pros:**
  - Global distribution and elastic scale
  - Flexible schema for evolving data models
  - Built-in multi-tenancy patterns
- **Cons:**
  - NoSQL model is a poor fit for the highly relational entity graph (equipment, maintenance, parts orders, alerts)
  - Complex cross-partition queries for reporting workloads
  - Significantly higher cost at the required consistency levels
  - Steep learning curve for the team

### Option 4: MySQL

- **Pros:**
  - Open-source and widely adopted
  - Azure Database for MySQL managed option available
- **Cons:**
  - Weaker window function support compared to SQL Server 2022
  - Less mature EF Core provider
  - Limited table partitioning capabilities relative to SQL Server
  - No full-text search parity with SQL Server for the parts catalog use case

## Consequences

### Positive

- Seamless integration with the existing ASP.NET Core and EF Core application stack
- Window functions enable efficient fleet utilization reports, maintenance trend analysis, and alert severity rankings without complex application-level logic
- Full-text search on the parts catalog reduces the need for a separate search engine in the initial release
- Table partitioning for TelemetryEvents provides a clear path for managing high-volume time-series data within the same database
- Azure SQL Database managed service reduces DBA overhead and provides built-in disaster recovery

### Negative

- SQL Server licensing costs are higher than open-source alternatives, though offset by the Azure SQL Database consumption model
- Team must stay current with SQL Server-specific features and migration paths
- Some advanced features (e.g., columnstore indexes for analytics) may require Enterprise tier pricing

### Risks

- If telemetry volume grows beyond SQL Server's practical limits, a dedicated time-series database may be needed (mitigated by the partitioning strategy in ADR-0003)
- Azure SQL Database DTU/vCore limits must be monitored and right-sized as fleet count grows
- Migration from EF Core InMemory to SQL Server may surface data type and constraint issues not caught during prototyping

## Implementation Notes

- The prototype currently uses EF Core InMemory provider; migration to SQL Server will be executed via EF Core migrations (see ADR-0002)
- Key indexes to create for performance:
  - `IX_Equipment_OrganizationId_SerialNumber` (unique) - multi-tenant equipment lookup
  - `IX_TelemetryEvents_EquipmentId_Timestamp` - time-range telemetry queries
  - `IX_Alerts_OrganizationId_Status_Severity` - alert dashboard filtering
  - `IX_Users_EntraObjectId` (unique) - Entra ID authentication lookup
- Connection strings will use Azure Key Vault references in production
- Azure SQL Database geo-replication to be configured for disaster recovery

## References

- Traces to: L1-002, L1-005, L1-009
- [SQL Server 2022 Documentation](https://learn.microsoft.com/en-us/sql/sql-server/)
- [Azure SQL Database Overview](https://learn.microsoft.com/en-us/azure/azure-sql/database/sql-database-paas-overview)
- [EF Core SQL Server Provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)
