# ADR-0004: SignalR for Real-Time Notifications

**Date:** 2026-04-01
**Category:** infrastructure
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Users of the Ironvale Fleet Hub need immediate notification of critical events including equipment faults, work order status changes, parts order updates, and AI-detected anomalies. The target delivery latency is under 5 seconds from event occurrence to user notification. Users should receive these notifications without needing to refresh the page. The notification system must also support multiple delivery channels (in-app, email, SMS) with user-configurable preferences per notification type. The system must scale to support concurrent connections from multiple organizations while maintaining message isolation between tenants.

## Decision

Use ASP.NET Core SignalR as the real-time notification hub for in-app notification delivery, combined with Azure Logic Apps for email and SMS dispatch:

- **SignalR Hub:** A `NotificationHub` hosted within the ASP.NET Core API provides real-time push notifications to connected browser clients with a target delivery latency of under 5 seconds.
- **Transport Fallback:** SignalR automatically negotiates the best available transport: WebSocket (preferred), Server-Sent Events (fallback), and long polling (last resort).
- **Group-Based Messaging:** Clients are organized into SignalR groups by organization (`org-{organizationId}`) and individual user (`user-{userId}`) for targeted message delivery.
- **Azure SignalR Service:** Used for production scaling, offloading connection management from the App Service to a dedicated managed service.
- **Multi-Channel Delivery:** Notification preferences stored per user control which channels (in-app, email, SMS) are active for each notification type. Email and SMS are dispatched via Azure Logic Apps integrated with Azure Communication Services.

## Options Considered

### Option 1: ASP.NET Core SignalR (chosen)

- **Pros:**
  - Native integration with ASP.NET Core; the hub is hosted within the existing API with shared authentication and dependency injection
  - Automatic transport fallback (WebSocket, SSE, long polling) ensures connectivity across diverse network environments including corporate proxies
  - Group-based messaging provides natural multi-tenant message isolation without custom routing logic
  - Azure SignalR Service provides a managed scaling path that offloads persistent connections from the App Service
  - Strong .NET client and JavaScript client library support
  - Integrates with the existing Entra ID bearer token authentication flow

- **Cons:**
  - Persistent connections increase memory usage per connected client on the App Service (mitigated by Azure SignalR Service)
  - SignalR message delivery is at-most-once; clients that reconnect may miss messages sent during disconnection
  - Azure SignalR Service adds a separate managed resource with its own pricing tier

### Option 2: Custom WebSocket Implementation

- **Pros:**
  - Full control over the protocol and message framing
  - No dependency on the SignalR library
  - Minimal abstraction overhead

- **Cons:**
  - Must implement connection management, heartbeats, reconnection, and group routing from scratch
  - No automatic transport fallback for environments where WebSocket is blocked
  - Significantly more development and testing effort for equivalent functionality
  - No managed scaling service equivalent to Azure SignalR Service

### Option 3: Server-Sent Events (SSE)

- **Pros:**
  - Simple unidirectional push model, easy to implement
  - Native browser support without additional libraries
  - Works through most corporate proxies

- **Cons:**
  - Unidirectional only (server to client); cannot receive client acknowledgments
  - Limited to text-based messages (no binary support)
  - No built-in group messaging or connection management
  - Maximum connection limit per domain in browsers (typically 6 per domain for HTTP/1.1)

### Option 4: Client Polling

- **Pros:**
  - Simplest implementation with standard HTTP requests
  - Works in all network environments
  - Stateless server; no persistent connections

- **Cons:**
  - Cannot meet the under-5-second delivery target without aggressive polling intervals that waste bandwidth
  - Polling at 1-second intervals for hundreds of users creates unnecessary server load
  - Poor user experience with variable latency between poll intervals
  - Inefficient use of network resources when there are no new notifications

### Option 5: Firebase Cloud Messaging

- **Pros:**
  - Purpose-built for push notifications with mobile support
  - Free tier covers substantial message volumes
  - Offline message queuing

- **Cons:**
  - Google dependency conflicts with the Azure-first platform decision (ADR-0001)
  - Primarily designed for mobile push; browser support via Web Push API is less mature
  - Does not integrate natively with ASP.NET Core authentication or Azure services
  - Additional vendor relationship and data processing agreement required

## Consequences

### Positive

- Users receive equipment fault alerts, work order changes, and parts order updates within 5 seconds of the triggering event
- Automatic transport negotiation ensures notifications work across corporate networks, VPNs, and environments where WebSocket may be blocked
- Group-based messaging (`org-{organizationId}`, `user-{userId}`) provides clean multi-tenant isolation without custom routing logic
- Azure SignalR Service provides a clear scaling path without re-architecting the notification system
- User preference controls allow individuals to customize their notification experience per channel and type, reducing notification fatigue

### Negative

- At-most-once delivery means clients may miss notifications during brief disconnections; mitigate by maintaining a notification inbox that clients query on reconnection to catch up on missed messages
- Azure SignalR Service is an additional managed resource with ongoing cost; monitor connection counts to right-size the tier
- SignalR adds persistent connection state that must be considered during App Service scaling events and deployments

### Risks

- If the number of concurrent connections exceeds the Azure SignalR Service tier capacity, notifications will be delayed or dropped; configure auto-scaling alerts on connection count metrics
- Network environments that block all real-time transports (WebSocket, SSE, long polling) will prevent in-app notifications; ensure email/SMS channels serve as reliable fallbacks for critical alerts
- SignalR reconnection storms after an App Service deployment could briefly spike connection broker load; implement staggered reconnection with jitter on the client

## Implementation Notes

- **NotificationHub** class inherits from `Hub` and is registered at `/hubs/notifications` in the ASP.NET Core middleware pipeline
- Authenticate SignalR connections using the same JWT bearer token scheme as the REST API; extract `organizationId` and `userId` from claims during `OnConnectedAsync` to assign group membership
- Notification types and their payloads:
  - `ServiceDueDate`: equipment ID, due date, work order ID
  - `EquipmentFault`: equipment ID, fault code, severity, timestamp
  - `WorkOrderChanged`: work order ID, old status, new status, changed by
  - `PartsOrderStatus`: order ID, new status, estimated delivery date
  - `AnomalyDetected`: equipment ID, anomaly type, confidence score, recommended action
- Implement a `NotificationService` that:
  1. Persists the notification to the `Notifications` table
  2. Sends real-time push via SignalR to the appropriate group
  3. Enqueues email/SMS dispatch via Azure Logic Apps if the user's preferences include those channels
- On client reconnection, query the `Notifications` endpoint for any notifications created after the client's last-seen timestamp
- Configure Azure SignalR Service in Default mode (hub hosted in the App Service, connections managed by the service)
- Set up Application Insights custom metrics for: notification delivery latency, active connections by organization, failed deliveries by channel

## References

- L1-008: Real-time notification and alerting requirements
- L1-015: Communication and notification infrastructure requirements
- ADR-0001: Azure Cloud Platform
- ADR-0003: Azure Logic Apps for Workflow Automation
- [ASP.NET Core SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [Azure SignalR Service](https://learn.microsoft.com/en-us/azure/azure-signalr/)
