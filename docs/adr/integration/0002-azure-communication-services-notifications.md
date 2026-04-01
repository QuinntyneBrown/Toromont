# ADR-0002: Azure Communication Services for Notifications

**Date:** 2026-04-01
**Category:** integration
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Toromont Fleet Hub requires a notification delivery system capable of sending both email and SMS messages to fleet managers, technicians, and administrators. Notification types include service reminders, equipment fault alerts, work order updates, parts order status changes, and AI-generated insights. Design 07 Section 8 Decision 2 evaluates notification delivery providers and selects Azure Communication Services (ACS) as the unified platform for both email and SMS channels. The system must integrate with Azure Logic Apps for workflow-driven notification dispatch and respect per-user notification preferences that control which channels and event types each user receives.

## Decision

Use Azure Communication Services (ACS) for email and SMS notification delivery, integrated via Azure Logic Apps connectors.

## Options Considered

### Option 1: Azure Communication Services (Chosen)
- **Pros:** Native Azure ecosystem integration with single billing, networking, and identity management; unified API for both email and SMS from a single service; no additional vendor relationship or contract to manage; built-in Azure Logic Apps connectors for workflow-driven dispatch; enterprise compliance with data residency within Azure; scales automatically with Azure infrastructure; supports Azure Monitor for delivery tracking and diagnostics.
- **Cons:** Newer service with a smaller community compared to Twilio; email features are less mature than dedicated email platforms; SMS availability may vary by region compared to Twilio's global reach.

### Option 2: Twilio (Email via SendGrid + SMS)
- **Pros:** Industry-leading SMS platform with global reach and reliability; SendGrid (now owned by Twilio) is a mature email delivery platform; extensive documentation and large developer community; proven at scale with billions of messages delivered.
- **Cons:** Requires a separate vendor relationship, contract, and billing outside the Azure ecosystem; two separate APIs (SendGrid for email, Twilio for SMS) despite common ownership; data leaves Azure, introducing additional network hops and compliance considerations; Logic Apps integration requires custom connectors or HTTP actions.

### Option 3: SendGrid + Separate SMS Provider
- **Pros:** SendGrid is a mature, well-documented email platform; flexibility to choose the best SMS provider independently.
- **Cons:** Two separate vendors to manage with separate billing and support; SendGrid is now part of Twilio, making this effectively a Twilio dependency; no unified API across channels; increased integration complexity.

### Option 4: Amazon SES + Amazon SNS
- **Pros:** Mature services with proven reliability; cost-effective at high volumes; comprehensive delivery metrics.
- **Cons:** Requires cross-cloud integration between Azure and AWS; introduces a second cloud provider dependency; networking complexity and latency for cross-cloud calls; complicates identity management and access control; conflicts with the Azure-first infrastructure decision (ADR infrastructure/0001).

### Option 5: Self-Hosted SMTP + SMS Gateway
- **Pros:** Full control over infrastructure and configuration; no vendor dependency for email delivery; potentially lower per-message cost at very high volumes.
- **Cons:** Significant operational overhead for managing SMTP servers, IP reputation, SPF/DKIM/DMARC configuration, and bounce handling; SMS gateways require carrier agreements; deliverability challenges without dedicated email reputation management; does not scale with managed service convenience; diverts engineering effort from core product development.

## Consequences

### Positive
- Unified Azure billing and management reduces vendor sprawl and simplifies procurement.
- Single SDK and API surface for both email and SMS reduces integration complexity and developer cognitive load.
- Azure Logic Apps connectors enable declarative, low-code notification workflows with built-in retry policies and error handling.
- Data stays within the Azure ecosystem, simplifying compliance with data residency and privacy requirements.
- Azure Monitor integration provides delivery tracking, failure alerts, and diagnostic logging without additional tooling.

### Negative
- Azure Communication Services is less mature than Twilio for SMS, with potential gaps in regional availability or advanced features.
- Email deliverability features (dedicated IPs, advanced reputation management) are less mature than specialized platforms like SendGrid.
- Tighter coupling to the Azure ecosystem makes future multi-cloud migration more complex.

### Risks
- ACS email or SMS delivery may experience regional availability issues as the service matures. Mitigation: monitor delivery rates and implement fallback alerting; maintain the ability to swap to an alternative provider behind the notification abstraction layer.
- SMS message limits or rate throttling may affect delivery during high-alert scenarios (e.g., multiple equipment faults simultaneously). Mitigation: implement priority queuing for critical alerts and batch non-urgent notifications.
- ACS pricing model changes could affect cost projections. Mitigation: monitor usage patterns and set up Azure Cost Management alerts.

## Implementation Notes
- Azure Logic Apps workflows dispatch email and SMS via ACS connectors, with retry policies configured per workflow.
- User notification preferences (stored in the NotificationPreference table) control which channels (email, SMS, in-app) and event types (service due, equipment fault, work order change, parts order status, AI insight) each user receives.
- Notification types include: service due date reminders, equipment fault alerts, work order status changes, parts order status updates, and AI-generated predictive maintenance insights.
- Logic Apps read user preferences before dispatching to avoid sending unwanted notifications.
- All notification deliveries are logged for audit and troubleshooting via Azure Monitor.
- A NotificationService abstraction in the backend allows swapping the delivery provider without changing business logic.

## References
- [L1-008: Notifications Design](../../design/L1-008-notifications.md)
- [Azure Communication Services Documentation](https://learn.microsoft.com/en-us/azure/communication-services/)
- [ADR infrastructure/0003: Azure Logic Apps Workflow Automation](../infrastructure/0003-azure-logic-apps-workflow-automation.md)
- [ADR infrastructure/0001: Azure Cloud Platform](../infrastructure/0001-azure-cloud-platform.md)
