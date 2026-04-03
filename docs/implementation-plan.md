# Plan: Implement Designs 14-22 with Two-Process Concurrent Execution

## Problem
Nine detailed designs (14-22) need to be implemented by two completely independent processes (Process A, Process B) running on different machines. They coordinate solely via git commits to `docs/agent-coordination.md`. Critical shared files (Program.cs, FleetHubDbContext.cs, NotificationDispatchService.cs) must not create merge conflicts.

## Split Strategy: File Ownership
- **Process A** owns all modifications to EXISTING shared files (refactoring, rewiring, hardening)
- **Process B** creates only NEW files, uses partial classes and extension methods to avoid touching files Process A owns

---

## Process A -- "Architecture Hardening + Notification Stack"
**Branch:** `process-a` | **Designs (5):** 17 -> 18 -> 14 -> 15 -> 20 (strictly sequential)

| Step | Design | Summary |
|------|--------|---------|
| A.1 | 17 -- CQRS Hardening | ProblemDetails exception handler, BusinessRuleException, validators for all write commands, LoggingBehavior enrichment, reflection-based validator coverage test |
| A.2 | 18 -- Tenant Filters | Rewrite FleetHubDbContext.OnModelCreating to fail-closed filters with CurrentOrganizationId property, cross-request isolation test |
| A.3 | 14 -- Alert Pipeline | Queue-triggered TelemetryAlertEvaluationFunction, Alert.SourceTelemetryEventId, remove API alert eval, new IronvaleFleetHub.Telemetry shared lib |
| A.4 | 15 -- Notification Contract | IHubAudienceResolver, hub refactor to canonical groups (user-{id}, org-{id}), NotificationDto unification, REST/SignalR event name alignment |
| A.5 | 20 -- Local Delivery | IEmailChannel/ISmsChannel impls, extend NotificationDispatchService constructor with optional channel params, CompositeEmailChannel, DevNotificationController |

**Why sequential:** Each step modifies files the next step depends on. Design 18 rewrites DbContext filters that 14 builds on. Design 15 refactors NotificationDispatchService that 20 extends.

**Files owned exclusively by Process A (may modify freely):**
- `Program.cs` -- exception handler, validator scan, remove AlertEvaluatorService, IHubAudienceResolver DI, channel DI
- `FleetHubDbContext.cs` -- OnModelCreating filter rewrite, Alert index, DeliveryAttempt/DevSms entity configs
- `NotificationDispatchService.cs` -- refactor event names (D15) + inject channels (D20)
- `NotificationHub.cs` -- group membership refactor
- `Behaviors/LoggingBehavior.cs` -- enrichment
- `Models/Alert.cs` -- SourceTelemetryEventId
- `Services/AlertEvaluatorService.cs` -- delete or repurpose
- `Functions/Functions/*` -- new evaluation function
- `Ironvale.sln` -- add Telemetry shared lib project
- All new files for D17, D15, D20: validators, exception types, channels, hub resolver, etc.

---

## Process B -- "Features, Frontend & Dev Tooling"
**Branch:** `process-b` | **Designs (4):** 16 // 21 (parallel), then 19, then 22

| Step | Design | Summary |
|------|--------|---------|
| B.1a | 16 -- Active Organization | GET /me/context, PUT /me/active-organization, frontend ActiveOrganizationService, interceptor, org switcher component |
| B.1b | 21 -- Local Dev AI | DevAiInsightsService, RuleBasedPredictionEngine, DevNaturalLanguageSearchService, SynonymCatalog, AiScenarioRecord entity, DevAiOptions config |
| B.2 | 19 -- Workflow Engine | DevWorkflowEngineHostedService, 3 workflow definitions (reminder, escalation, parts-order), WorkflowRunRecord + WorkflowDispatchRecord entities, DevWorkflowController |
| B.3 | 22 -- Functions Local Dev | local.settings.example.json, POST /api/dev/telemetry/ingest bypass endpoint, DevTelemetryController, frontend environment.development.ts |

**Why this grouping:** All designs create NEW files in isolated directories. B.1a and B.1b have zero file overlap and run in parallel. B.2 and B.3 create files in separate directories.

**Files owned exclusively by Process B (may modify freely):**
- `Features/Me/*` -- new query + command handlers for active org
- `Features/Workflows/*` -- new workflow definitions + services
- `Services/AI/*` -- new AI services (8+ files)
- `Controllers/DevWorkflowController.cs`, `Controllers/DevTelemetryController.cs` -- new
- `Models/AiScenarioRecord.cs`, `Models/WorkflowRunRecord.cs`, `Models/WorkflowDispatchRecord.cs` -- new
- `Data/FleetHubDbContext.DevEntities.cs` -- NEW partial class (see protocol below)
- `Data/DataSeeder.cs` -- append AI scenarios + parts-order events (Process A does NOT touch this)
- `Extensions/DevServicesRegistration.cs` -- NEW extension methods (see protocol below)
- ALL Frontend files: interceptor, services, org switcher, environment config
- `Functions/local.settings.example.json` -- new file

---

## Shared File Protocol -- Zero-Conflict Convention

### 1. FleetHubDbContext.cs -- Partial Class Pattern
- **Process A** modifies the main file (filter rewrite, Alert index, DeliveryAttemptRecord/DevSmsMessageRecord configs)
- **Process B** creates `Data/FleetHubDbContext.DevEntities.cs` as a partial class:
  ```csharp
  // FleetHubDbContext.DevEntities.cs
  public partial class FleetHubDbContext
  {
      public DbSet<AiScenarioRecord> AiScenarioRecords => Set<AiScenarioRecord>();
      public DbSet<WorkflowRunRecord> WorkflowRunRecords => Set<WorkflowRunRecord>();
      public DbSet<WorkflowDispatchRecord> WorkflowDispatchRecords => Set<WorkflowDispatchRecord>();
  }
  ```
- **Process B** creates `IEntityTypeConfiguration<T>` classes per entity (auto-discovered by `ApplyConfigurationsFromAssembly`)
- **Result:** ZERO merge conflicts -- Process A edits main file, Process B creates new files

### 2. Program.cs -- Extension Method Pattern
- **Process A** modifies the main file (exception handler middleware, validator assembly scan, remove AlertEvaluatorService, add IHubAudienceResolver, add channel DI in dev block)
- **Process B** creates `Extensions/DevServicesRegistration.cs`:
  ```csharp
  public static class DevServicesRegistration
  {
      public static IServiceCollection AddDevAiServices(this IServiceCollection s, IConfiguration c) { ... }
      public static IServiceCollection AddDevWorkflowServices(this IServiceCollection s, IConfiguration c) { ... }
  }
  ```
- **Merge phase** adds one line to Program.cs dev block: `builder.Services.AddDevAiServices(config).AddDevWorkflowServices(config);`
- **Result:** ZERO merge conflicts -- Process A edits main file, Process B creates new files

### 3. appsettings.Development.json
- **Process A** adds `DevNotificationDelivery` config section (Design 20)
- **Process B** adds `DevAi` (Design 21), `DevWorkflows` (Design 19) config sections
- **Result:** LOW risk -- different JSON keys; git auto-merge handles adjacent additions

### 4. NotificationDispatchService.cs
- **Process A owns exclusively.** Design 15 refactors event names. Design 20 injects channels. Both sequential within Process A.
- **Process B does NOT touch this file.**

---

## Dependency Graph
```
Process A (sequential):          Process B (parallel start):

  A.1 [17-CQRS]                   B.1a [16-Org] ---+
       |                           B.1b [21-AI] ---+
       v                                            |
  A.2 [18-Filters]                                  v
       |                           B.2 [19-Workflows]
       v                                |
  A.3 [14-Pipeline]                     v
       |                           B.3 [22-FuncDev]
       v                                |
  A.4 [15-Notif]                        v
       |                           B.Done -----------+
       v                                             |
  A.5 [20-Delivery]                                  v
       |                                      [MERGE PHASE]
       v                                             ^
  A.Done ------------------------------------------- +
```

---

## Merge Phase
Triggered when BOTH processes report "Complete" in coordination file:
1. Merge `process-a` to `main` (first -- owns existing file modifications)
2. Merge `process-b` to `main` (only new files + appsettings additions -- minimal conflicts)
3. Wire Process B's `DevServicesRegistration` into Program.cs dev block (1 line)
4. Verify `IEntityTypeConfiguration<T>` auto-discovery via `ApplyConfigurationsFromAssembly`
5. Run `dotnet build Ironvale.sln` + `dotnet test`
6. Run frontend build: `cd src/frontend && npx ng build --configuration=development`
7. Update `docs/detailed-designs/00-index.md` designs 14-22 status to "Implemented"

---

## Coordination File Protocol
Both processes read/write `docs/agent-coordination.md` via git:
1. On start: `git pull`, read coordination file for current state
2. Before each design: update own row status to "In Progress", commit + push
3. After each design: update own row status to "Done", commit + push
4. On completion: set process status to "Complete -- ready for merge"
5. Merge phase: triggered when BOTH processes show "Complete"

## Build & Test Commands
```bash
# Full solution build
dotnet build Ironvale.sln

# Backend integration tests
dotnet test tests/backend/IronvaleFleetHub.Api.IntegrationTests/ --configuration Release

# Frontend build (dev mode)
cd src/frontend && npx ng build --configuration=development
```

## Git Conventions
- Branch: `process-a` or `process-b`
- Commit prefix: `feat(design-{N}): {description}`
- Trailer: `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`

## Todos (SQL-tracked)
See `todos` table for per-design tracking with process assignment and dependencies.
