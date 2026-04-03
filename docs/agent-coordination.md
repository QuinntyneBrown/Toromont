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
**Branch:** `process-a` | **Machine:** TBD | **Process Status:** Complete -- ready for merge

Designs execute **strictly sequentially** (each modifies files the next depends on).

| Step | Design | Status | Files Created/Modified | Notes |
|------|--------|--------|----------------------|-------|
| A.1 | 17 -- CQRS Hardening | Done | **MODIFY:** Program.cs (exception handler content-type fix). Added ISkipValidation to all 27 query types. | All 26 tests passing. ProblemDetails returns application/problem+json. |
| A.2 | 18 -- Tenant Filters | Done | **MODIFY:** Data/FleetHubDbContext.cs (fail-closed query filters, CurrentOrganizationId/TenantResolved properties). **NEW:** TenantQueryFilterTests.cs (2 isolation tests) | Fail-closed tenant filters. 26 tests passing. |
| A.3 | 14 -- Alert Pipeline | Done | **MODIFY:** Models/Alert.cs (SourceTelemetryEventId), FleetHubDbContext.cs (Alert index), Program.cs (removed IAlertEvaluatorService DI), IngestTelemetryCommand.cs (removed alert eval), Ironvale.sln. **NEW:** IronvaleFleetHub.Telemetry/ (shared lib), TelemetryAlertEvaluationFunction.cs | Alert evaluation moved to queue-triggered function. 26 tests passing. |
| A.4 | 15 -- Notification Contract | Done | **MODIFY:** Hubs/NotificationHub.cs (IHubAudienceResolver rewrite), Services/NotificationDispatchService.cs (NotificationDto payload), Program.cs (IHubAudienceResolver DI). **NEW:** Services/IHubAudienceResolver.cs, Services/HubAudienceResolver.cs, DTOs/NotificationDto.cs | Canonical groups: user-{id}, org-{id}. Unified NotificationDto. 26 tests passing. |
| A.5 | 20 -- Local Delivery | Done | **MODIFY:** Services/NotificationDispatchService.cs (optional IEmailChannel/ISmsChannel injection), FleetHubDbContext.cs (DeliveryAttemptRecord + DevSmsMessageRecord configs), Program.cs (channel DI in dev block), appsettings.Development.json (DevNotificationDelivery). **NEW:** IDeliveryChannel.cs, DevSmtpEmailChannel.cs, FileDropEmailChannel.cs, CompositeEmailChannel.cs, ConsoleSmsChannel.cs, NotificationTemplateRenderer.cs, NotificationPreferenceEvaluator.cs, DevNotificationController.cs, DeliveryAttemptRecord.cs, DevSmsMessageRecord.cs, NotificationEnvelope.cs | CompositeEmailChannel: SMTP→file-drop fallback. 26 tests passing. |

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
