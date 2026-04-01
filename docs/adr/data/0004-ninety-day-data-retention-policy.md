# ADR-0004: 90-Day Data Retention Policy

**Date:** 2026-04-01
**Category:** data
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Toromont Fleet Hub generates significant volumes of transient data across three domains: raw telemetry events from equipment sensors (Design 05 Decision 1), AI predictions that have been dismissed or have expired (Design 06 Decision 5), and notifications that have been read by users (Design 07 Decision 3). Without a retention policy, these tables will grow unbounded, degrading query performance and increasing storage costs. The dashboard time-range selector supports up to 90 days of historical data for trend analysis, establishing a natural retention boundary. A single nightly cleanup job is preferred over per-entity scheduled tasks to simplify operations and reduce the number of scheduled jobs to monitor.

## Decision

Implement a 90-day retention policy for raw telemetry events, dismissed/expired AI predictions, and read notifications, enforced by a single nightly SQL Agent cleanup job.

## Options Considered

### Option 1: 90-Day Retention with Nightly Cleanup Job (Chosen)
- **Pros:** 90 days aligns with the dashboard's maximum time-range selector, ensuring all visible data is retained; balances storage costs with data availability for trend analysis; single nightly job simplifies operations and monitoring; aligns with the telemetry partitioning strategy (ADR data/0003) enabling efficient partition switch-out for telemetry cleanup; predictable storage growth makes capacity planning straightforward.
- **Cons:** Data older than 90 days is permanently deleted and cannot be recovered without backups; 90 days may be insufficient for long-term trend analysis or regulatory requirements; nightly job creates a brief period of increased database load.

### Option 2: No Retention (Keep Everything)
- **Pros:** All historical data is available for analysis at any time; no risk of accidentally deleting useful data; simplest implementation with no cleanup logic.
- **Cons:** Unbounded table growth degrades query performance over time; storage costs increase continuously; telemetry tables could grow to billions of rows within a year; index maintenance becomes increasingly expensive; backups take longer and consume more storage.

### Option 3: 30-Day Retention
- **Pros:** More aggressive storage savings; faster cleanup operations on smaller datasets.
- **Cons:** Insufficient for the 90-day dashboard time-range selector, which would show gaps; too short for meaningful trend analysis across seasonal equipment usage patterns; users would lose access to recent historical data they expect to be available.

### Option 4: 1-Year Retention
- **Pros:** Generous retention window supports long-term trend analysis; aligns with annual reporting cycles.
- **Cons:** Significantly higher storage costs, especially for high-volume telemetry data; diminishing analytical value for raw telemetry beyond 90 days (aggregated data serves long-term analysis better); larger tables slow down queries even with indexing; cleanup of a year's worth of data in a single job could be disruptive.

### Option 5: Per-Entity Configurable Retention
- **Pros:** Maximum flexibility to tune retention per data type; different stakeholders can set different policies.
- **Cons:** Increased operational complexity with multiple retention configurations to manage; harder to reason about storage growth; more complex cleanup logic; over-engineering for the current requirement where 90 days suits all three entity types.

## Consequences

### Positive
- Predictable and bounded storage growth across telemetry, AI prediction, and notification tables.
- Query performance remains consistent as tables do not grow unbounded.
- Single nightly job is easy to monitor, alert on, and troubleshoot.
- For partitioned TelemetryEvents, partition switch-out provides near-instantaneous cleanup without row-by-row deletion overhead.
- 90-day retention satisfies the dashboard time-range selector requirement, ensuring users always see complete data within the supported range.

### Negative
- Raw telemetry data older than 90 days is permanently deleted. Long-term trend analysis must rely on pre-aggregated summary tables rather than raw events.
- If the dashboard time-range selector is extended beyond 90 days in the future, the retention policy must be updated accordingly.
- The nightly cleanup job adds a scheduled dependency that must be monitored for failures.

### Risks
- Cleanup job failure could lead to unbounded table growth if not detected promptly. Mitigation: configure SQL Agent alerts for job failures and monitor table sizes via Azure Monitor.
- Batched DELETE operations on non-partitioned tables (AIPredictions, Notifications) could cause lock contention during peak hours. Mitigation: run the job during off-peak hours (nightly) and use batched deletes with small batch sizes (e.g., 5,000 rows per batch) with brief pauses between batches.
- Regulatory or audit requirements may mandate longer data retention for certain notification types. Mitigation: review retention policy with compliance stakeholders before go-live; archive critical notifications to cold storage if required.

## Implementation Notes
- A single nightly SQL Agent job runs cleanup across all three tables:
  - **TelemetryEvents** (>90 days old): Uses partition switch-out for efficient bulk deletion, aligned with the monthly partitioning strategy from ADR data/0003.
  - **AIPredictions** (dismissed or expired, >90 days old): Uses batched DELETE (e.g., 5,000 rows per batch) to avoid long-running transactions and excessive lock escalation.
  - **Notifications** (read, >90 days old): Uses batched DELETE with the same batch size strategy.
- The job logs the number of rows deleted per table for auditing and monitoring.
- Schedule the job during the lowest-activity window (e.g., 2:00 AM local time).
- Configure SQL Agent alerts to notify the operations team if the job fails or takes longer than expected.
- Consider adding a pre-aggregation step before deletion if long-term trend analysis requirements emerge (e.g., roll up daily telemetry averages into a summary table).

## References
- [L1-005: Telemetry Design](../../design/L1-005-telemetry.md)
- [L1-006: AI Predictions Design](../../design/L1-006-ai-predictions.md)
- [L1-008: Notifications Design](../../design/L1-008-notifications.md)
- [ADR data/0003: Telemetry Time-Series Data Strategy](0003-telemetry-time-series-data-strategy.md)
