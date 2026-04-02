# ADR-0001: Event-Driven Architecture for Telemetry, Workflows, and Notifications

**Date:** 2026-04-01
**Category:** general
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

The Ironvale Fleet Hub platform has multiple asynchronous processing concerns that span different domains and operate at different scales. Telemetry ingestion must handle high-volume data streams from fleet equipment sensors. Service workflows require automated reminders and escalation logic that execute on schedules and in response to state changes. Real-time notifications must reach users within seconds of relevant events occurring. AI processing pipelines (predictive maintenance, anomaly detection) run as batch jobs that consume telemetry data and produce alerts. A synchronous request-response architecture cannot efficiently address these diverse requirements without creating tight coupling, bottlenecks, and poor user experience.

## Decision

Adopt an event-driven architecture for telemetry processing, service workflows, and real-time notifications using Azure-native services.

### Implementation Architecture

- **Azure Functions:** Handle telemetry ingestion at scale (event-triggered) and predictive maintenance batch processing (timer-triggered, daily at 2 AM UTC). Functions scale independently based on event volume.
- **Azure Logic Apps:** Orchestrate service reminder workflows with notifications at 7 days, 3 days, and 1 day before scheduled service. Implement 48-hour escalation logic when service acknowledgments are not received.
- **SignalR:** Deliver real-time notifications to connected users with a target latency of less than 5 seconds from event occurrence to user notification.
- **Retry Policies:** All event processors implement exponential backoff retry policies. Messages that fail after maximum retries are routed to dead letter queues for manual investigation and reprocessing.

## Options Considered

### Option 1: Event-Driven Architecture with Azure Services (chosen)

- **Pros:**
  - Decouples event producers from consumers, allowing each to evolve independently
  - Enables independent scaling: telemetry ingestion scales separately from the API tier and notification delivery
  - Supports complex, multi-step workflows (service reminders at 7/3/1 days, 48-hour escalation) declaratively via Logic Apps
  - Real-time user experience through SignalR push notifications
  - Dead letter queues provide resilience and auditability for failed event processing
  - Natural fit for the Azure ecosystem already in use

- **Cons:**
  - Increased architectural complexity compared to synchronous approaches
  - Distributed system debugging requires correlation IDs and structured logging
  - Eventual consistency must be accounted for in the user experience
  - More infrastructure components to monitor and maintain

### Option 2: Synchronous Request-Response for Everything

- **Pros:**
  - Simplest architecture with straightforward request-response patterns
  - Easier debugging and tracing of individual requests
  - Strong consistency guarantees

- **Cons:**
  - Cannot handle high-volume telemetry ingestion without blocking API resources
  - Long-running workflows (reminders, escalations) cannot be modeled as synchronous requests
  - No real-time notification capability without polling
  - Tight coupling between all system components
  - Poor scalability: API tier must scale to handle telemetry volume

### Option 3: Message Queue (RabbitMQ/Kafka)

- **Pros:**
  - Battle-tested messaging infrastructure
  - Kafka provides excellent throughput for high-volume telemetry streams
  - Strong ordering guarantees (Kafka partitions)

- **Cons:**
  - Requires self-managed infrastructure (or managed service at additional cost)
  - Does not natively provide workflow orchestration (reminders, escalations)
  - No built-in real-time client notification; still needs SignalR or equivalent
  - Operational overhead for cluster management, partition rebalancing, and monitoring
  - Duplicates capabilities already available in the Azure ecosystem

### Option 4: CQRS with Event Sourcing

- **Pros:**
  - Complete audit trail of all state changes via event store
  - Natural separation of read and write models for performance optimization
  - Enables temporal queries and state reconstruction

- **Cons:**
  - Significantly higher implementation complexity, especially for event store management
  - Steep learning curve for the development team
  - Event schema evolution is challenging and requires careful versioning
  - Overkill for the current requirements; the system does not need full event sourcing capabilities
  - Increased storage costs for maintaining the complete event history

## Consequences

### Positive

- Telemetry ingestion scales elastically via Azure Functions without impacting API performance or availability
- Service workflows (reminders and escalations) are modeled declaratively in Logic Apps, making them easy to understand, modify, and audit
- Users receive real-time notifications within 5 seconds of relevant events, providing an responsive experience
- Independent scaling of each concern (ingestion, processing, notification) optimizes cost and performance
- Dead letter queues ensure no events are silently lost, supporting operational reliability
- The architecture naturally accommodates future event-driven features without structural changes

### Negative

- Distributed event flows are harder to trace and debug than synchronous call chains; requires investment in correlation IDs and distributed tracing
- Eventual consistency means the UI may briefly show stale data after writes; the team must design UX patterns to handle this gracefully
- More Azure services to provision, configure, monitor, and pay for
- Testing event-driven workflows end-to-end requires integration test infrastructure that simulates event flows

### Risks

- Azure Functions cold start latency could delay telemetry processing during traffic spikes. Mitigated by using Premium plan with pre-warmed instances for critical functions.
- Logic Apps workflow failures could cause missed service reminders or escalations. Mitigated by monitoring Logic Apps run history and alerting on failures.
- SignalR connection drops could cause missed real-time notifications. Mitigated by implementing client-side reconnection logic with exponential backoff and a notification inbox for catching up on missed events.
- Dead letter queue growth could indicate systemic processing failures. Mitigated by monitoring dead letter queue depth with alerts and establishing operational runbooks for reprocessing.

## Implementation Notes

- All events must include a correlation ID for distributed tracing across Azure Functions, Logic Apps, and SignalR.
- Azure Functions for telemetry ingestion should use consumption plan for cost efficiency, with Premium plan reserved for latency-sensitive functions.
- Logic Apps service reminder schedule: notifications at T-7 days, T-3 days, and T-1 day before scheduled service date. Escalation triggers 48 hours after a service reminder is sent without acknowledgment.
- SignalR hub should be implemented as an Azure SignalR Service (managed) to avoid self-hosting overhead.
- Retry policy: exponential backoff starting at 1 second (1s, 4s, 16s), maximum 3 retries, with jitter to prevent thundering herd. Messages exceeding max retries are routed to the dead letter queue.
- Dead letter queue messages should retain the original event payload, failure reason, retry count, and timestamps for diagnosis.

## References

- L1-015: Event-Driven Processing
- L1-005: Service and Maintenance Management
- L1-008: Notifications and Alerts
- Azure Functions documentation: https://learn.microsoft.com/en-us/azure/azure-functions/
- Azure Logic Apps documentation: https://learn.microsoft.com/en-us/azure/logic-apps/
- Azure SignalR Service documentation: https://learn.microsoft.com/en-us/azure/azure-signalr/
