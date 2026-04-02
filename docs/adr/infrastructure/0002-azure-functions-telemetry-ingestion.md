# ADR-0002: Azure Functions for Telemetry Ingestion

**Date:** 2026-04-01
**Category:** infrastructure
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Equipment in the Ironvale fleet sends periodic telemetry data including engine hours, fuel levels, temperature readings, and GPS coordinates. The system must ingest over 100 events per second during peak periods, with the ingestion layer scaling independently of the main API to avoid degrading user-facing request performance. The ingestion pipeline must handle burst traffic from fleet-wide reporting windows, persist data reliably, and support bulk database inserts for efficiency. Additionally, a daily predictive maintenance function must run against accumulated telemetry data to generate AI-driven maintenance predictions.

## Decision

Use Azure Functions for telemetry data ingestion and scheduled predictive maintenance processing:

- **Telemetry Ingestion Function:** HTTP-triggered function that accepts telemetry event batches, validates payloads, and performs bulk inserts into the database using Dapper. Designed to process 100+ events per second with independent scaling.
- **Retry Policy:** 3 retries with exponential backoff (1 second, 4 seconds, 16 seconds) on database errors before routing to the dead letter queue.
- **Dead Letter Queue:** Failed events are stored in a `TelemetryDeadLetterQueue` for manual investigation and replay.
- **Predictive Maintenance Function:** Timer-triggered function that runs daily at 2:00 AM UTC, analyzing telemetry patterns to generate maintenance predictions via Azure OpenAI.

## Options Considered

### Option 1: Azure Functions (chosen)

- **Pros:**
  - Serverless scaling automatically handles burst traffic without pre-provisioning capacity
  - Independent scaling from the main API means telemetry spikes do not affect user-facing endpoints
  - Consumption-based pricing keeps costs proportional to actual telemetry volume
  - Native Azure integration with Application Insights, Key Vault, and Azure SQL
  - Supports Dapper for efficient bulk insert operations
  - Timer triggers provide built-in cron scheduling for the predictive maintenance function
  - Built-in retry policies and dead letter queue patterns

- **Cons:**
  - Cold start latency on the consumption plan can add 1-2 seconds to the first request after idle
  - Execution timeout limits (10 minutes on consumption plan) require careful batch sizing
  - Debugging serverless functions locally requires Azure Functions Core Tools setup

### Option 2: Main API Endpoint

- **Pros:**
  - Simpler architecture with a single deployment artifact
  - No additional infrastructure to manage
  - Shared authentication and middleware with existing endpoints

- **Cons:**
  - Telemetry burst traffic would compete with user-facing API requests for resources
  - Cannot scale telemetry ingestion independently of the main API
  - Auto-scaling the entire App Service to handle telemetry peaks is more expensive than scaling a single function
  - Risk of degrading dashboard and work order API response times during peak telemetry windows

### Option 3: Azure Event Hubs + Stream Analytics

- **Pros:**
  - Purpose-built for high-throughput event streaming at millions of events per second
  - Stream Analytics provides real-time windowed aggregations
  - Durable event storage with configurable retention

- **Cons:**
  - Over-engineered for the current scale of 100+ events per second
  - Significantly higher base cost due to throughput unit provisioning
  - Added architectural complexity with multiple services to configure and monitor
  - Team would need to learn Event Hubs partitioning and Stream Analytics query language

### Option 4: AWS Lambda

- **Pros:**
  - Mature serverless platform with broad language support
  - Provisioned concurrency option eliminates cold starts

- **Cons:**
  - Cross-cloud deployment adds operational complexity and network latency
  - No native integration with Azure SQL, Application Insights, or Entra ID
  - Contradicts the Azure-first platform decision (ADR-0001)
  - Separate billing and monitoring tooling required

## Consequences

### Positive

- Telemetry ingestion scales independently, ensuring user-facing API performance is unaffected by telemetry burst traffic
- Consumption-based pricing means costs are directly proportional to the volume of telemetry events processed
- The retry policy with exponential backoff provides resilience against transient database failures without overwhelming the database during recovery
- Dead letter queue ensures no telemetry data is silently lost; failed events can be investigated and replayed
- The predictive maintenance function runs during off-peak hours (2:00 AM UTC), minimizing resource contention
- Dapper bulk inserts maximize database write throughput while keeping memory allocation efficient

### Negative

- Cold start latency on the consumption plan may cause the first telemetry batch after an idle period to take longer; mitigate by configuring a warm-up ping or upgrading to the Premium plan if latency is unacceptable
- Separate deployment pipeline required for Azure Functions alongside the main API
- Monitoring and troubleshooting spans two compute platforms (App Services and Functions)

### Risks

- If telemetry volume grows beyond consumption plan limits, migration to the Azure Functions Premium plan will be needed; monitor invocation counts and duration metrics
- Dead letter queue growth could indicate a systemic database issue; set up alerts when the queue depth exceeds a threshold (e.g., 100 messages)
- Predictive maintenance function depends on Azure OpenAI availability; implement circuit breaker pattern to handle service degradation gracefully

## Implementation Notes

- Configure the telemetry ingestion function with a maximum batch size of 500 events per invocation to stay within execution time limits
- Use Dapper `SqlBulkCopy` wrapper for bulk inserts into the `TelemetryEvents` table
- Implement idempotency using the telemetry event's `EquipmentId + Timestamp` composite key to handle duplicate submissions from retries
- Set up Application Insights custom metrics for: events ingested per minute, retry count, dead letter queue depth
- Configure the predictive maintenance timer trigger with the cron expression `0 0 2 * * *` (daily at 2:00 AM UTC)
- Store Azure SQL connection strings and Azure OpenAI API keys in Azure Key Vault with managed identity access
- Implement health check endpoint on the function app for monitoring

## References

- L1-005: Telemetry data ingestion and processing requirements
- L1-015: Serverless and background processing requirements
- ADR-0001: Azure Cloud Platform
- [Azure Functions Best Practices](https://learn.microsoft.com/en-us/azure/azure-functions/functions-best-practices)
- [Dapper Bulk Insert Patterns](https://github.com/DapperLib/Dapper)
