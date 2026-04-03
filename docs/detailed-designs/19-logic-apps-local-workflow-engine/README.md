# Logic Apps Local Workflow Engine — Detailed Design

## 1. Overview

**Source:** `docs/local-development-strategy.md` Section 4.1 identifies Azure Logic Apps as a service without a complete local alternative available today.

The production architecture uses Azure Logic Apps for recurring service reminders, overdue work order escalations, and workflow-driven notification fan-out. That design is appropriate in Azure, but it leaves local development with no fully integrated way to execute, inspect, retry, and test the same workflows on a developer machine.

**Scope:** introduce a development-only local workflow engine that runs inside the ASP.NET Core host as an `IHostedService`, executes the same reminder and escalation rules as the cloud workflows, simulates inbound parts-order fulfillment updates, records run history, and exposes manual trigger hooks for deterministic testing.

**Goals:**

- Preserve the local inner loop with zero Azure dependency
- Keep workflow rules explicit and testable in C#
- Support the same functional outcomes as the production Logic Apps workflows
- Provide local run history, idempotency, and retry behavior that are good enough for developer verification

**References:**

- [Local Development Strategy](../../local-development-strategy.md)
- [ADR-0003 Azure Logic Apps Workflow Automation](../../adr/infrastructure/0003-azure-logic-apps-workflow-automation.md)
- [Feature 03 Service Management](../03-service-management/README.md)
- [Feature 07 Notifications & Reporting](../07-notifications-reporting/README.md)

## 2. Architecture

### 2.1 Runtime Components

The local replacement has five main runtime elements:

- `LocalWorkflowEngineHostedService` schedules and executes development workflows
- `IWorkflowDefinition` implementations contain each workflow's business logic
- `WorkflowRunRepository` stores run history, attempts, and last error state
- `WorkflowDispatchLogRepository` stores idempotency keys so retries do not duplicate side effects
- downstream application services perform notifications and work-order updates using the same contracts as the production app

![Component Diagram](diagrams/c4_component.png)

### 2.2 Canonical Execution Flow

1. The ASP.NET Core API starts in `Development`.
2. `LocalWorkflowEngineHostedService` loads enabled workflow definitions and checks which are due.
3. The due workflow queries the application database for candidates.
4. The workflow computes a deterministic dispatch key per action.
5. New actions are executed through existing application services.
6. The engine writes a `WorkflowRunRecord` and updates the workflow cursor.

![Sequence Diagram](diagrams/sequence_workflow_run.png)

### 2.3 Class Diagram

![Class Diagram](diagrams/class_diagram.png)

## 3. Components, Types, and Classes

### 3.1 Core Abstractions

#### `IWorkflowEngine`

- **Type:** application service interface
- **Responsibility:** exposes manual triggering and status inspection for development tooling and tests
- **Key members:**
  - `Task TriggerAsync(string workflowName, CancellationToken ct = default)`
  - `Task<IReadOnlyList<WorkflowStatusDto>> GetStatusAsync(CancellationToken ct = default)`

#### `IWorkflowDefinition`

- **Type:** workflow contract
- **Responsibility:** defines one executable workflow unit
- **Key members:**

```csharp
public interface IWorkflowDefinition
{
    string Name { get; }
    TimeSpan PollInterval { get; }
    bool Enabled(LocalWorkflowOptions options);
    Task ExecuteAsync(WorkflowExecutionContext context, CancellationToken ct);
}
```

Each definition owns one domain concern and should not branch across unrelated workflow families.

#### `IWorkflowRunRepository`

- **Type:** persistence abstraction
- **Responsibility:** stores execution start/end times, attempt count, status, and error details
- **Used by:** hosted service, controller, tests

#### `IWorkflowDispatchLogRepository`

- **Type:** idempotency abstraction
- **Responsibility:** records dispatch keys such as `service-reminder:{workOrderId}:3d` or `escalation:{workOrderId}:high`
- **Used by:** each workflow before creating side effects

#### `IWorkflowClock`

- **Type:** time abstraction
- **Responsibility:** makes schedule evaluation and tests deterministic
- **Default implementation:** `SystemWorkflowClock`

### 3.2 Hosted Runtime

#### `LocalWorkflowEngineHostedService`

- **Type:** `BackgroundService`
- **Responsibility:** drives the scheduler loop in development only
- **Behavior:**
  - loads all registered `IWorkflowDefinition` instances
  - checks due state using `PollInterval` and last successful run
  - executes workflows serially by default to keep local behavior predictable
  - records success/failure in `WorkflowRunRepository`

This service replaces the schedule trigger aspect of Logic Apps locally.

#### `WorkflowExecutionCoordinator`

- **Type:** orchestration service
- **Responsibility:** wraps one workflow execution with logging, retries, idempotency wiring, and correlation IDs
- **Why separate:** keeps `BackgroundService` small and lets tests execute one workflow without starting the full host

#### `DevWorkflowController`

- **Type:** development-only API controller
- **Responsibility:** manual trigger and inspection endpoints
- **Recommended routes:**
  - `POST /api/dev/workflows/{name}/trigger`
  - `GET /api/dev/workflows`
  - `GET /api/dev/workflows/runs?name=ServiceReminderWorkflow`

This controller is guarded behind `IHostEnvironment.IsDevelopment()` and should not be registered in non-development environments.

### 3.3 Workflow Definitions

#### `ServiceReminderWorkflow`

- **Type:** `IWorkflowDefinition`
- **Responsibility:** finds scheduled service work orders due in 7, 3, and 1 day windows
- **Inputs:** due date, assigned technician, fleet manager, notification preferences
- **Outputs:** in-app notification, optional local email, optional local SMS
- **Idempotency key:** `service-reminder:{workOrderId}:{offsetDays}`

#### `WorkOrderEscalationWorkflow`

- **Type:** `IWorkflowDefinition`
- **Responsibility:** identifies open work orders overdue by more than 48 hours and escalates priority one level at a time
- **Inputs:** work order age, current status, priority, supervisor assignment
- **Outputs:** work order priority update plus escalation notifications
- **Idempotency key:** `work-order-escalation:{workOrderId}:{newPriority}`

#### `PartsOrderStatusWorkflow`

- **Type:** `IWorkflowDefinition`
- **Responsibility:** simulates the Logic App that would process fulfillment status updates from an external system
- **Inputs:** pending parts orders plus a local source of status events
- **Outputs:** order status changes, timeline entries, and notifications to the ordering user
- **Local source:** JSON fixture file or development table such as `DevPartsOrderEvents`
- **Idempotency key:** `parts-order-status:{externalEventId}`

### 3.4 Supporting Services

#### `ReminderCandidateQueryService`

- **Type:** query service
- **Responsibility:** loads reminder candidates with enough data to evaluate channel preferences and recipients

#### `EscalationPolicyService`

- **Type:** domain service
- **Responsibility:** computes the next priority level and prevents escalation past `Critical`

#### `LocalPartsOrderEventSource`

- **Type:** infrastructure adapter
- **Responsibility:** reads simulated fulfillment updates from local fixtures or a dev table
- **Why needed:** local development still needs an event source even though no external vendor is present

#### `INotificationDispatchService`

- **Type:** existing application boundary
- **Responsibility:** remains the single way workflows create inbox notifications and downstream email or SMS work
- **Integration:** this design depends on [Communication Services Local Delivery](../20-communication-services-local-delivery/README.md) for development email/SMS behavior

### 3.5 Data Contracts and Option Types

#### `WorkflowExecutionContext`

- **Type:** execution DTO
- **Fields:**
  - `Guid RunId`
  - `DateTime StartedAtUtc`
  - `string CorrelationId`
  - `IServiceProvider Services`
  - `LocalWorkflowOptions Options`

#### `WorkflowRunRecord`

- **Type:** persistence model
- **Fields:**
  - `Guid Id`
  - `string WorkflowName`
  - `DateTime StartedAtUtc`
  - `DateTime? CompletedAtUtc`
  - `string Status`
  - `int Attempt`
  - `string? Error`

#### `WorkflowDispatchRecord`

- **Type:** idempotency model
- **Fields:**
  - `string DispatchKey`
  - `string WorkflowName`
  - `DateTime CreatedAtUtc`
  - `string EntityType`
  - `Guid EntityId`

#### `LocalWorkflowOptions`

- **Type:** options class
- **Purpose:** toggles workflows and controls polling

```csharp
public sealed class LocalWorkflowOptions
{
    public bool Enabled { get; set; }
    public bool EnableServiceReminders { get; set; }
    public bool EnableWorkOrderEscalation { get; set; }
    public bool EnablePartsOrderStatusSync { get; set; }
    public int SchedulerPeriodSeconds { get; set; } = 60;
    public int MaxRetryCount { get; set; } = 3;
}
```

## 4. Detailed Behavior

### 4.1 Scheduling Model

- The host wakes up every `SchedulerPeriodSeconds`.
- Each workflow definition declares its own `PollInterval`.
- The engine checks `lastSuccess + PollInterval <= now`.
- Missed runs after a restart execute once; the engine does not replay every missed interval.

This intentionally favors a predictable local loop over perfect cloud parity.

### 4.2 Idempotency Model

- Every externally visible action uses a deterministic dispatch key.
- Before sending notifications or mutating a work order, the workflow checks `IWorkflowDispatchLogRepository`.
- If the key already exists, the action is skipped and the run is still marked successful.

This protects local development from duplicate reminders when a developer restarts the API or manually re-triggers a workflow.

### 4.3 Retry Model

- Transient failures retry in-process up to `MaxRetryCount`.
- Retries happen within the same scheduler tick.
- Final failure writes the exception text to `WorkflowRunRecord.Error`.
- A later scheduler tick may attempt the workflow again.

### 4.4 Observability

Each run logs:

- workflow name
- correlation ID
- candidate count
- actions created
- actions skipped for idempotency
- elapsed time
- error text on failure

The log format should align with Serilog structured logging already used by the API.

## 5. Acceptance Tests

### 5.1 Reminder Workflow

- Given a work order due in 3 days, when `ServiceReminderWorkflow` runs, then exactly one reminder is recorded and the same run does not produce duplicates on retry.

### 5.2 Escalation Workflow

- Given an open work order older than 48 hours with `Medium` priority, when `WorkOrderEscalationWorkflow` runs, then the priority becomes `High` and one escalation notification is created.

### 5.3 Parts Status Workflow

- Given a local fulfillment event `Shipped`, when `PartsOrderStatusWorkflow` runs, then the parts order status is updated once and the event is not re-applied on the next run.

### 5.4 Manual Trigger Endpoint

- Given a development environment, when `POST /api/dev/workflows/ServiceReminderWorkflow/trigger` is called, then the workflow executes immediately and a new `WorkflowRunRecord` is visible in the run history endpoint.

## 6. Security Considerations

- The entire local workflow engine is registered only in `Development`.
- Manual workflow endpoints must not be reachable in production builds.
- Local run history should avoid storing full SMS or email bodies if those payloads contain sensitive content already stored elsewhere.
- Fixture-driven parts status events should be treated as trusted development data only.

## 7. Open Questions

1. Should workflow run history live in SQL tables, or is structured logging alone sufficient for v1 local development?
2. Should the parts-order local event source be file-based, table-based, or both?
3. Is serial workflow execution sufficient, or do developers need opt-in parallel execution for high-volume local load testing?
