# Agent Coordination -- Designs 14-22 Implementation

> **Two-Process Model:** Process A and Process B run on separate machines, each on its own git branch.
> They coordinate ONLY via this file. Each process pulls this file before starting a design and pushes updates after completing one.

## Protocol
1. **Before starting a design:** `git pull origin main`, read this file, update your row to "In Progress", commit + push
2. **After completing a design:** update your row to "Done", commit + push
3. **On process completion:** set process status to "Complete -- ready for merge"
4. **File ownership is absolute:** never modify a file owned by the other process (see Shared File Protocol below)

## Status Legend
| Status | Meaning |
|--------|---------|
| Queued | Not started, waiting for previous step or dependencies |
| In Progress | Agent is actively writing code |
| Building | Running `dotnet build` to verify compilation |
| Testing | Running `dotnet test` to verify no regressions |
| Done | Completed, committed to process branch |
| Blocked | Cannot proceed -- see Notes column |

---

## Process A -- Architecture Hardening + Notification Stack
**Branch:** `process-a` | **Machine:** TBD | **Process Status:** Queued

Designs execute **strictly sequentially** (each modifies files the next depends on).

| Step | Design | Status | Files Created/Modified | Notes |
|------|--------|--------|----------------------|-------|
| A.1 | 17 -- CQRS Hardening | Queued | **MODIFY:** Program.cs (exception handler + validator scan), Behaviors/LoggingBehavior.cs. **NEW:** Middleware/ProblemDetailsExceptionHandler.cs, Common/BusinessRuleException.cs, Features/*/Validators/*.cs (10-15 validators), integration test for validator coverage | Must write validators for ALL existing write commands. Reflection test enforces coverage. |
| A.2 | 18 -- Tenant Filters | Queued | **MODIFY:** Data/FleetHubDbContext.cs (rewrite OnModelCreating filter section lines 262-281, add CurrentOrganizationId + TenantResolved properties). **NEW:** Cross-request isolation integration test | CRITICAL: this rewrites filter logic that all subsequent DbContext changes depend on |
| A.3 | 14 -- Alert Pipeline | Queued | **MODIFY:** Models/Alert.cs (add SourceTelemetryEventId), FleetHubDbContext.cs (Alert index config), Program.cs (remove IAlertEvaluatorService DI), Ironvale.sln (add project ref). **NEW:** src/backend/IronvaleFleetHub.Telemetry/ (shared lib), Functions/Functions/TelemetryAlertEvaluationFunction.cs, queue message models | Removes AlertEvaluatorService from API DI. Adds queue-triggered function. |
| A.4 | 15 -- Notification Contract | Queued | **MODIFY:** Hubs/NotificationHub.cs (OnConnectedAsync rewrite), Services/NotificationDispatchService.cs (event names lines 64-83), Program.cs (IHubAudienceResolver DI). **NEW:** Services/IHubAudienceResolver.cs, Services/HubAudienceResolver.cs, DTOs/NotificationDto.cs | Canonical groups: user-{id}, org-{id}. Unify NotificationDto for REST + SignalR. |
| A.5 | 20 -- Local Delivery | Queued | **MODIFY:** Services/NotificationDispatchService.cs (add IEmailChannel?/ISmsChannel? constructor params, replace log-only hooks lines 88-102), FleetHubDbContext.cs (DevSmsMessageRecord + DeliveryAttemptRecord entity configs), Program.cs (channel DI in dev block), appsettings.Development.json (DevNotificationDelivery section). **NEW:** Services/IEmailChannel.cs, Services/ISmsChannel.cs, Services/DevSmtpEmailChannel.cs, Services/FileDropEmailChannel.cs, Services/CompositeEmailChannel.cs, Services/ConsoleSmsChannel.cs, Services/NotificationTemplateRenderer.cs, Services/NotificationPreferenceEvaluator.cs, Controllers/DevNotificationController.cs, Models/DevSmsMessageRecord.cs, Models/DeliveryAttemptRecord.cs | Extends NotificationDispatchService refactored in A.4. CompositeEmailChannel: SMTP first, file-drop fallback. |

---

## Process B -- Features, Frontend & Dev Tooling
**Branch:** `process-b` | **Machine:** TBD | **Process Status:** Queued

Steps B.1a and B.1b run **in parallel** (zero file overlap). B.2 and B.3 are sequential after B.1.

| Step | Design | Status | Files Created/Modified | Notes |
|------|--------|--------|----------------------|-------|
| B.1a | 16 -- Active Organization | Queued | **NEW:** Features/Me/Queries/GetCurrentUserContextQuery.cs + handler, Features/Me/Commands/SetActiveOrganizationCommand.cs + handler, Controllers/MeController.cs, Models/CurrentUserContextResponse.cs, Frontend: services/active-organization.service.ts, interceptors/active-organization.interceptor.ts, components/org-switcher/* | Parallel with B.1b. Backend: 2 new endpoints. Frontend: service + interceptor + org switcher UI. |
| B.1b | 21 -- Local Dev AI | Queued | **NEW:** Services/AI/DevAiInsightsService.cs, Services/AI/RuleBasedPredictionEngine.cs, Services/AI/NarrativeFormatter.cs, Services/AI/DevNaturalLanguageSearchService.cs, Services/AI/TokenVectorizer.cs, Services/AI/SynonymCatalog.cs, Services/AI/AiProviderSelector.cs, Services/AI/OllamaAiInsightsService.cs, Models/AiScenarioRecord.cs, Data/Configurations/AiScenarioRecordConfiguration.cs. **MODIFY:** Data/DataSeeder.cs (append AI scenarios), appsettings.Development.json (DevAi section) | Parallel with B.1a. Static dictionary synonym catalog. Rule-based predictions. Uses partial class + IEntityTypeConfiguration for DbContext integration. |
| B.2 | 19 -- Workflow Engine | Queued | **NEW:** Features/Workflows/DevWorkflowEngineHostedService.cs, Features/Workflows/IWorkflowDefinition.cs, Features/Workflows/ServiceReminderWorkflow.cs, Features/Workflows/WorkOrderEscalationWorkflow.cs, Features/Workflows/PartsOrderStatusWorkflow.cs, Features/Workflows/IWorkflowClock.cs, Controllers/DevWorkflowController.cs, Models/WorkflowRunRecord.cs, Models/WorkflowDispatchRecord.cs, Data/Configurations/WorkflowRunRecordConfiguration.cs, Data/Configurations/WorkflowDispatchRecordConfiguration.cs. **MODIFY:** Data/DataSeeder.cs (append parts-order events), appsettings.Development.json (DevWorkflows section) | 3 workflows: reminder, escalation, parts-order. Serial execution. Manual trigger endpoints. |
| B.3 | 22 -- Functions Local Dev | Queued | **NEW:** Functions/local.settings.example.json, Controllers/DevTelemetryController.cs. **MODIFY:** Frontend environments/environment.development.ts (telemetry endpoint + API key) | Bypass endpoint: POST /api/dev/telemetry/ingest. Primarily config + documentation. |

---

## Shared File Protocol -- Zero-Conflict Rules

**CRITICAL: These rules prevent merge conflicts between Process A and Process B.**

| Shared File | Process A Responsibility | Process B Responsibility | Conflict Risk |
|-------------|------------------------|------------------------|---------------|
| `Program.cs` | MODIFY directly: exception handler, validator scan, remove AlertEvaluatorService, IHubAudienceResolver DI, channel DI | DO NOT MODIFY. Create `Extensions/DevServicesRegistration.cs` with extension methods instead. Merge phase wires it in. | **ZERO** |
| `FleetHubDbContext.cs` | MODIFY directly: rewrite OnModelCreating filters, Alert index, entity configs for DeliveryAttempt/DevSms | DO NOT MODIFY. Create `Data/FleetHubDbContext.DevEntities.cs` (partial class) + `IEntityTypeConfiguration<T>` classes | **ZERO** |
| `NotificationDispatchService.cs` | MODIFY directly: refactor event names (D15) + inject channels (D20) | DO NOT TOUCH | **ZERO** |
| `appsettings.Development.json` | Add `DevNotificationDelivery` section | Add `DevAi` + `DevWorkflows` sections | **LOW** (different JSON keys) |
| `DataSeeder.cs` | DO NOT TOUCH | MODIFY: append AI scenarios + parts-order events at end | **ZERO** |
| `Ironvale.sln` | MODIFY: add IronvaleFleetHub.Telemetry project | DO NOT TOUCH | **ZERO** |

### Process B File Patterns (for zero-conflict integration)

**Partial DbContext** -- `Data/FleetHubDbContext.DevEntities.cs`:
```csharp
public partial class FleetHubDbContext
{
    public DbSet<AiScenarioRecord> AiScenarioRecords => Set<AiScenarioRecord>();
    public DbSet<WorkflowRunRecord> WorkflowRunRecords => Set<WorkflowRunRecord>();
    public DbSet<WorkflowDispatchRecord> WorkflowDispatchRecords => Set<WorkflowDispatchRecord>();
}
```

**Extension Methods** -- `Extensions/DevServicesRegistration.cs`:
```csharp
public static class DevServicesRegistration
{
    public static IServiceCollection AddDevAiServices(this IServiceCollection services, IConfiguration config) { ... }
    public static IServiceCollection AddDevWorkflowServices(this IServiceCollection services, IConfiguration config) { ... }
}
```

**Entity Configurations** -- `Data/Configurations/{Entity}Configuration.cs`:
```csharp
public class AiScenarioRecordConfiguration : IEntityTypeConfiguration<AiScenarioRecord>
{
    public void Configure(EntityTypeBuilder<AiScenarioRecord> builder) { ... }
}
```

---

## Merge Phase Checklist
Triggered when BOTH processes report "Complete":

- [ ] Merge `process-a` to `main` (first -- owns existing file modifications)
- [ ] Merge `process-b` to `main` (new files + appsettings -- minimal conflicts)
- [ ] Resolve any `appsettings.Development.json` merge conflicts (trivial -- different keys)
- [ ] Add to Program.cs dev block: `builder.Services.AddDevAiServices(config).AddDevWorkflowServices(config);`
- [ ] Verify `ApplyConfigurationsFromAssembly` discovers Process B's entity configurations
- [ ] Run `dotnet build Ironvale.sln`
- [ ] Run `dotnet test tests/backend/IronvaleFleetHub.Api.IntegrationTests/ --configuration Release`
- [ ] Run `cd src/frontend && npx ng build --configuration=development`
- [ ] Update `docs/detailed-designs/00-index.md` designs 14-22 status to "Implemented"

---

*Last updated: 2026-04-03T05:53Z*
*Process A Status: Queued*
*Process B Status: Queued*
