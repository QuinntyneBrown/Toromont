# ADR-0003: SQL Server Table Partitioning with Dapper for Telemetry Time-Series Data

**Date:** 2026-04-01
**Category:** data
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Equipment in the Toromont Fleet Hub sends telemetry data every 6 hours, reporting engine hours, fuel level, temperature, and GPS coordinates. The system must handle ingestion rates of 100+ events per second during peak periods (e.g., fleet-wide reporting cycles), store a rolling window of 30+ days of raw telemetry per equipment, and serve efficient time-range queries for dashboard charts with selectable windows (24h, 7d, 30d, 90d, custom). The TelemetryEvents table will grow to millions of rows and requires a data management strategy that balances write throughput, query performance, and data retention without introducing additional infrastructure beyond the primary SQL Server database established in ADR-0001.

## Decision

Use SQL Server table partitioning by month on the TelemetryEvents table, combined with Dapper for high-throughput bulk insert and read operations, and a composite index optimized for the dominant query pattern of equipment-specific time-range lookups.

## Options Considered

### Option 1: SQL Server Table Partitioning with Dapper (chosen)

- **Pros:**
  - Keeps all data in SQL Server, maintaining a single database to manage, back up, and monitor (aligned with ADR-0001)
  - Table partitioning by month enables partition elimination on date-range queries, dramatically reducing I/O for time-windowed dashboard charts
  - Dapper bulk inserts via SqlBulkCopy handle 100+ events/sec throughput without the overhead of EF Core change tracking
  - Composite index `IX_TelemetryEvents_EquipmentId_Timestamp` directly supports the most common query pattern: "get telemetry for equipment X between time A and time B"
  - Old partitions can be switched out efficiently for data retention without row-by-row deletes
  - Partition-level statistics improve query optimizer decisions for time-range predicates
  - No additional infrastructure, licensing, or operational expertise required beyond SQL Server
- **Cons:**
  - Table partitioning requires SQL Server Enterprise edition or Azure SQL Database (Business Critical or Hyperscale tier)
  - Partition management (creating new monthly partitions, archiving old ones) requires scheduled maintenance jobs
  - Dapper introduces a second data access pattern alongside EF Core, increasing codebase complexity
  - SQL Server is not purpose-built for time-series workloads and may not match the query performance of specialized engines at extreme scale

### Option 2: Azure Time Series Insights

- **Pros:**
  - Purpose-built for IoT and telemetry time-series data
  - Native time-series query language and built-in visualization
  - Automatic data tiering and retention management
- **Cons:**
  - Introduces a separate data store, increasing operational complexity and cost
  - Data must be synchronized or queried across two systems for reports that join telemetry with equipment or maintenance data
  - Azure Time Series Insights Gen1 is deprecated; Gen2 has been replaced by Azure Data Explorer, adding migration risk
  - Additional team training required

### Option 3: InfluxDB

- **Pros:**
  - Industry-leading open-source time-series database
  - Optimized write throughput and compression for time-series data
  - Built-in downsampling and retention policies
- **Cons:**
  - Introduces a completely separate database engine to deploy, manage, and monitor
  - No native integration with EF Core or the .NET ecosystem
  - Cross-database joins with SQL Server for reporting would require application-level data federation
  - Operational expertise for InfluxDB is not present on the team

### Option 4: Azure Cosmos DB with Time-Series Pattern

- **Pros:**
  - Elastic scale with guaranteed low-latency writes
  - TTL-based automatic data expiration
  - Global distribution if needed in the future
- **Cons:**
  - Significantly higher cost for the write volume and storage requirements
  - Partition key design for time-series is non-trivial and error-prone
  - Aggregation queries (averages, trends) are limited compared to SQL window functions
  - Another database to manage alongside SQL Server

### Option 5: Plain SQL Server without Partitioning

- **Pros:**
  - Simplest implementation; standard table with indexes
  - Works on any SQL Server edition
  - No partition management overhead
- **Cons:**
  - Full table scans on date-range queries as the table grows beyond tens of millions of rows
  - Data retention requires expensive DELETE operations that generate massive transaction logs
  - Index maintenance (rebuilds, statistics updates) becomes increasingly slow on a monolithic table
  - Query performance degrades predictably as data volume grows

## Consequences

### Positive

- Single database simplifies backup, monitoring, disaster recovery, and compliance posture
- Monthly partitions align naturally with the dashboard time-range selectors (24h, 7d, 30d, 90d) and enable partition elimination for all common query patterns
- Dapper bulk inserts provide the throughput needed for peak ingestion without saturating EF Core's change tracker
- Partition switching enables near-instant archival of old data without impacting ongoing operations
- The composite index `IX_TelemetryEvents_EquipmentId_Timestamp` serves as a covering index for the primary query pattern, minimizing lookups

### Negative

- Two data access patterns (EF Core for domain entities, Dapper for telemetry) increase cognitive load and require clear architectural boundaries
- Partition management automation must be built and maintained (monthly partition creation job, archival job)
- Azure SQL Database tier selection must account for the partitioning feature requirement

### Risks

- If telemetry frequency increases beyond 6-hour intervals (e.g., real-time streaming at 1-second intervals), SQL Server partitioning may not provide sufficient write throughput; at that point, a dedicated streaming/time-series solution (Azure Data Explorer, Event Hubs) would be required
- Partition function and scheme changes require careful planning and testing to avoid data loss
- Dead letter queue for failed telemetry events must be monitored; unprocessed events could indicate systemic ingestion failures that degrade dashboard accuracy

## Implementation Notes

- Partition function: `CREATE PARTITION FUNCTION pf_TelemetryByMonth (datetime2) AS RANGE RIGHT FOR VALUES ('2026-01-01', '2026-02-01', ...)` with a scheduled job to add future monthly boundaries
- Partition scheme maps the function to filegroups (or a single filegroup on Azure SQL Database)
- Dapper-based `TelemetryRepository` handles:
  - Bulk insert via `SqlBulkCopy` for ingestion batches
  - Parameterized queries for time-range reads with `EquipmentId` and `Timestamp` filters
  - Aggregation queries (hourly/daily averages) for downsampled chart data
- Dead letter queue: failed telemetry events are written to a `TelemetryDeadLetterQueue` table with error details for retry or manual investigation
- Data retention policy (open question, proposed):
  - Raw telemetry: retain for 1 year, then archive to cold storage or delete
  - Aggregated telemetry (hourly/daily summaries): retain for 5 years for long-term trend analysis
  - Retention enforcement via monthly partition switch-out to an archive table, followed by truncation or export to Azure Blob Storage
- Query optimization: for the 90-day and custom range selectors, queries should include `OPTION (RECOMPILE)` or use parameterized plan guides to avoid parameter sniffing issues across vastly different date ranges

## References

- Traces to: L1-005, L1-015
- [SQL Server Table Partitioning](https://learn.microsoft.com/en-us/sql/relational-databases/partitions/partitioned-tables-and-indexes)
- [Dapper Documentation](https://github.com/DapperLib/Dapper)
- [SqlBulkCopy Class](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy)
- ADR-0001: SQL Server 2022 as Primary Relational Database
