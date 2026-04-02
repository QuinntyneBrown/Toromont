# ADR-0003: Azure Logic Apps for Workflow Automation

**Date:** 2026-04-01
**Category:** infrastructure
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

The Ironvale Fleet Hub's work order management system requires automated workflows for service reminders and escalation handling. Service reminders must be sent at 7 days, 3 days, and 1 day before a scheduled service due date to ensure technicians and fleet managers are aware of upcoming maintenance. Additionally, overdue work orders must be automatically escalated after 48 hours to prevent maintenance tasks from falling through the cracks. These workflows need to be maintainable by operations staff and modifiable without requiring code deployments.

## Decision

Use Azure Logic Apps for service reminder and escalation workflows:

- **Service Reminder Workflow:** Triggers on a recurring schedule, queries work orders with upcoming due dates, and sends notifications at 7-day, 3-day, and 1-day intervals before the service due date via in-app notification, email, and SMS channels based on user preferences.
- **Escalation Workflow:** Triggers on a recurring schedule, identifies work orders that have been overdue for more than 48 hours without resolution, and automatically escalates them by updating the work order priority and notifying the appropriate supervisor.

## Options Considered

### Option 1: Azure Logic Apps (chosen)

- **Pros:**
  - Visual workflow designer allows non-developer operations staff to inspect and modify workflows
  - Built-in connectors for email (Azure Communication Services), SMS, and HTTP endpoints reduce custom integration code
  - Built-in retry policies with configurable intervals handle transient failures
  - No infrastructure management; fully managed serverless execution
  - Schedules can be modified without code deployment through the Azure portal
  - Run history provides visibility into workflow execution for troubleshooting
  - Native integration with Azure Monitor for alerting on workflow failures

- **Cons:**
  - Logic Apps designer has a learning curve for complex conditional logic
  - Per-action pricing can become expensive at very high execution volumes
  - Limited unit testing support compared to code-based approaches
  - Vendor lock-in to the Azure Logic Apps execution model

### Option 2: Custom Background Services (IHostedService)

- **Pros:**
  - Full control over execution logic in familiar C# code
  - Runs within the existing App Service deployment, no additional infrastructure
  - Easy to unit test with standard .NET testing frameworks
  - No per-action pricing

- **Cons:**
  - Tightly coupled to the API deployment lifecycle; schedule changes require code deployment
  - No visual representation of workflow steps for operations staff
  - Must implement retry logic, error handling, and execution logging manually
  - Scaling is tied to the App Service plan rather than independent

### Option 3: Hangfire

- **Pros:**
  - Mature .NET library for background job scheduling
  - Dashboard UI for monitoring job execution
  - Supports recurring jobs with cron expressions
  - Persistent job storage in SQL Server

- **Cons:**
  - Adds a dependency and dashboard that must be secured and maintained
  - Runs within the App Service process, competing for resources with the API
  - No built-in connectors for email/SMS; would need custom integration code
  - Dashboard is developer-oriented, not designed for operations staff

### Option 4: Azure Durable Functions

- **Pros:**
  - Code-based orchestration with support for long-running workflows
  - Built-in patterns for fan-out/fan-in, chaining, and human interaction
  - Serverless pricing model with independent scaling

- **Cons:**
  - Requires developer involvement for all workflow changes
  - No visual designer for operations staff
  - More complex programming model (orchestrator replay semantics)
  - Over-engineered for the relatively simple reminder and escalation patterns needed

## Consequences

### Positive

- Operations staff can inspect workflow run history and understand execution flow through the visual designer without developer assistance
- Schedule modifications (e.g., changing reminder intervals from 7/3/1 days to 10/5/2 days) can be made through the Azure portal without code deployment
- Built-in retry policies ensure transient failures in email/SMS delivery are handled automatically
- Workflow execution history provides an audit trail of all reminder and escalation actions taken
- Independent scaling means workflow execution does not compete with user-facing API resources

### Negative

- Two deployment targets to manage: Logic Apps workflows in addition to the main API and Azure Functions
- Complex conditional logic (e.g., checking user notification preferences per channel) may be harder to express in the visual designer than in C# code
- Per-action billing requires monitoring to ensure costs remain within budget as work order volume grows

### Risks

- Logic Apps connector deprecation or breaking changes during Azure updates; mitigate by pinning connector versions and testing after Azure updates
- If the number of work orders grows significantly, the recurring schedule-based polling pattern may need to be replaced with event-driven triggers; monitor execution duration and consider switching to Service Bus triggers
- Operations staff modifying workflows without adequate understanding could introduce errors; mitigate with a change approval process and staging environment for testing

## Implementation Notes

- **Service Reminder Workflow:**
  - Recurrence trigger: runs daily
  - Query the API for work orders with due dates falling within the 7-day, 3-day, and 1-day windows
  - For each matching work order, check the assigned user's notification preferences
  - Dispatch notifications via the appropriate channels (in-app via SignalR, email via Azure Communication Services, SMS via Azure Communication Services)
  - Log each notification sent to the `Notifications` table with timestamp and channel

- **Escalation Workflow:**
  - Recurrence trigger: runs every hour
  - Query the API for work orders where status is Open and `CreatedAt + 48 hours < current time` (stuck in Open for more than 48 hours)
  - Automatically escalate by updating the work order priority level (Low → Medium → High → Critical)
  - Notify the assigned user's supervisor via in-app and email channels
  - Log escalation action to the `Notifications` table

- Configure managed identity for Logic Apps to authenticate against the Fleet Hub API
- Set up Azure Monitor alerts for workflow failure rates exceeding 5% over a 1-hour window
- Use Logic Apps Standard (single-tenant) for network isolation within the Azure virtual network

## References

- L1-003: Work order management and scheduling requirements
- L1-015: Workflow automation and background processing requirements
- ADR-0001: Azure Cloud Platform
- [Azure Logic Apps Documentation](https://learn.microsoft.com/en-us/azure/logic-apps/)
- [Logic Apps Standard vs. Consumption](https://learn.microsoft.com/en-us/azure/logic-apps/single-tenant-overview-compare)
