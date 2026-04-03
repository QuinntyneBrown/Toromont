# Agent Coordination — Designs 14–22 Implementation

> **Protocol:** Every agent MUST read this file before starting work, update their status row as they progress, and list files they are actively modifying. Before touching any file listed in another agent's "Files Owned" column, STOP and check if that agent's status is "Done". If not, coordinate via the Blockers column.

## Status Legend
| Status | Meaning |
|--------|---------|
| Queued | Not started, waiting for dependencies |
| In Progress | Agent is actively writing code |
| Building | Agent is running `dotnet build` to verify compilation |
| Testing | Agent is running `dotnet test` to verify no regressions |
| Done | Agent completed, committed, and pushed to feature branch |
| Blocked | Agent cannot proceed — see Blockers column |

---

## Phase 1 — Independent Foundations

| Agent | Design | Branch | Status | Files Owned | Blockers | Notes |
|-------|--------|--------|--------|-------------|----------|-------|
| A1-CQRS | 17 — CQRS & Validation Hardening | `feature/design-17-cqrs-hardening` | Queued | `Behaviors/LoggingBehavior.cs`, `Common/BusinessRuleException.cs`, `Common/Result.cs`, `Features/*/Validators/*`, `Middleware/ProblemDetailsExceptionHandler.cs` | None | Adds global exception handler to Program.cs, FluentValidation assembly scan. Must write validators for ALL existing write commands. Reflection test enforces coverage. |
| A2-AI | 21 — Local Development AI | `feature/design-21-local-ai` | Queued | `Services/AI/*`, `Models/AiScenarioRecord.cs` | None | Adds DevAiInsightsService, RuleBasedPredictionEngine, DevNaturalLanguageSearchService, SynonymCatalog, TokenVectorizer, NarrativeFormatter. Adds AiScenarioRecord entity to DbContext. Adds AI scenario seed data to DataSeeder. Adds DevAi config section to appsettings.Development.json. Conditional DI in Program.cs: `if (builder.Environment.IsDevelopment())`. |
| A3-Org | 16 — Active Organization Alignment | `feature/design-16-active-org` | Queued | `Features/Users/Queries/GetUserContextQuery.cs`, `Features/Users/Commands/SetActiveOrganizationCommand.cs`, `Controllers/UsersController.cs` (new endpoints only), Frontend: `core/services/active-organization.service.ts`, `core/interceptors/active-organization.interceptor.ts`, `shared/components/header/` | None | Backend: GET /api/v1/me/context + PUT /api/v1/me/active-organization. Frontend: ActiveOrganizationService, activeOrganizationInterceptor (replaces tenantInterceptor), org switcher in header. No DB schema changes. |

### Phase 1 Shared File Merge Plan
After all three agents are Done, merge branches to main in order: A1-CQRS → A2-AI → A3-Org.

| Shared File | A1-CQRS Changes | A2-AI Changes | A3-Org Changes | Conflict Risk |
|-------------|-----------------|---------------|----------------|---------------|
| `Program.cs` | Add `app.UseExceptionHandler()` + `services.AddValidatorsFromAssembly()` | Add `if (IsDevelopment()) { services.AddSingleton<IAiProvider, DevAiInsightsService>(); ... }` | None (endpoints auto-discovered via controller) | Low — different sections |
| `FleetHubDbContext.cs` | None | Add `DbSet<AiScenarioRecord>` | None | None |
| `DataSeeder.cs` | None | Add AI scenario records | None | None |
| `appsettings.Development.json` | None | Add `DevAi` config section | None | None |
| Integration tests | Add validator coverage reflection test | None | Add org-switching test | None — different test files |

---

## Phase 2 — Core Hardening

| Agent | Design | Branch | Status | Files Owned | Blockers | Notes |
|-------|--------|--------|--------|-------------|----------|-------|
| A4-Filters | 18 — Tenant Query Filter Hardening | `feature/design-18-tenant-filters` | Queued | `Data/FleetHubDbContext.cs` (OnModelCreating filter section), `Data/FleetHubDbContext.CurrentOrganizationId.cs` (partial) | Waiting: Phase 1 merge | Refactor all query filters to fail-closed: `TenantResolved && x.OrganizationId == CurrentOrganizationId`. Add CurrentOrganizationId + TenantResolved properties to DbContext. Cross-request isolation integration test. |
| A5-Pipeline | 14 — Telemetry Alert Pipeline | `feature/design-14-alert-pipeline` | Queued | `Functions/Functions/TelemetryAlertEvaluationFunction.cs`, `Functions/Models/TelemetryAlertEvaluationMessage.cs`, `Models/Alert.cs` (add SourceTelemetryEventId), new `src/backend/IronvaleFleetHub.Telemetry/` shared project | Waiting: Phase 1 merge | Queue-triggered function evaluates alerts. Azure Storage Queue transport. Idempotency via SourceTelemetryEventId unique index. Remove API-side IAlertEvaluatorService DI. Dead-letter via queue-native poison queue. |

### Phase 2 Shared File Merge Plan
Merge order: A4-Filters → A5-Pipeline.

| Shared File | A4-Filters Changes | A5-Pipeline Changes | Conflict Risk |
|-------------|-------------------|---------------------|---------------|
| `Program.cs` | None | Remove `services.AddScoped<IAlertEvaluatorService>()` | None |
| `FleetHubDbContext.cs` | Rewrite filter section + add properties | Add SourceTelemetryEventId to Alert config | Medium — A4 rewrites filters, A5 adds entity config. Merge A4 first. |
| `Ironvale.sln` | None | Add IronvaleFleetHub.Telemetry project reference | None |

---

## Phase 3 — Notification & Functions Config

| Agent | Design | Branch | Status | Files Owned | Blockers | Notes |
|-------|--------|--------|--------|-------------|----------|-------|
| A6-Notif | 15 — Notification Contract Unification | `feature/design-15-notification-contract` | Queued | `Hubs/NotificationHub.cs`, `Services/NotificationDispatchService.cs`, `Services/IHubAudienceResolver.cs`, `DTOs/NotificationDto.cs`, Frontend: `core/services/auth.service.ts` (token retrieval), `shared/components/notification-*` | Waiting: Phase 2 merge | Refactor hub to use canonical groups (user-{id}, org-{id}). IHubAudienceResolver resolves from JWT. Unify NotificationDto for REST + SignalR. Frontend: authenticated SignalR connection with bearer token. |
| A7-FuncDev | 22 — Azure Functions Local Dev | `feature/design-22-functions-local-dev` | Queued | `Functions/local.settings.example.json`, `Controllers/DevTelemetryController.cs`, Frontend: `environments/environment.development.ts` | Waiting: Phase 2 merge | Create local.settings.example.json (committed). Add POST /api/dev/telemetry/ingest bypass endpoint. Update frontend dev environment config. Primarily documentation and config. |

### Phase 3 Shared File Merge Plan
Merge order: A6-Notif → A7-FuncDev.

| Shared File | A6-Notif Changes | A7-FuncDev Changes | Conflict Risk |
|-------------|-----------------|-------------------|---------------|
| `Program.cs` | None (dispatch service already registered) | Add dev telemetry bypass endpoint DI | None |
| `appsettings.Development.json` | None | Add Functions config section | None |
| Frontend `AuthService` | Add `getAccessToken()` method | None | None |
| Frontend `environment.development.ts` | None | Add telemetry endpoint URL + API key | None |

---

## Phase 4 — Local Development Tooling

| Agent | Design | Branch | Status | Files Owned | Blockers | Notes |
|-------|--------|--------|--------|-------------|----------|-------|
| A8-Workflows | 19 — Logic Apps Local Workflow Engine | `feature/design-19-workflow-engine` | Queued | `Features/Workflows/*`, `Controllers/DevWorkflowController.cs`, `Models/WorkflowRunRecord.cs`, `Models/WorkflowDispatchRecord.cs`, `Services/IWorkflowClock.cs` | Waiting: Phase 3 merge | DevWorkflowEngineHostedService with 3 workflows: ServiceReminderWorkflow, WorkOrderEscalationWorkflow, PartsOrderStatusWorkflow. Manual trigger endpoints. WorkflowRunRecord + WorkflowDispatchRecord entities. Serial execution. IWorkflowClock for deterministic tests. |
| A9-Delivery | 20 — Communication Services Local Delivery | `feature/design-20-local-delivery` | Queued | `Services/Notifications/IEmailChannel.cs`, `Services/Notifications/DevSmtpEmailChannel.cs`, `Services/Notifications/FileDropEmailChannel.cs`, `Services/Notifications/CompositeEmailChannel.cs`, `Services/Notifications/ISmsChannel.cs`, `Services/Notifications/ConsoleSmsChannel.cs`, `Services/Notifications/NotificationTemplateRenderer.cs`, `Controllers/DevNotificationController.cs`, `Models/DevSmsMessageRecord.cs`, `Models/DeliveryAttemptRecord.cs` | Waiting: Phase 3 merge | Extend NotificationDispatchService with optional IEmailChannel? + ISmsChannel? constructor params. CompositeEmailChannel: SMTP first, file-drop fallback. ConsoleSmsChannel: logs + DB entity. DevNotificationController for inspection. |

### Phase 4 Shared File Merge Plan
Merge order: A8-Workflows → A9-Delivery.

| Shared File | A8-Workflows Changes | A9-Delivery Changes | Conflict Risk |
|-------------|---------------------|---------------------|---------------|
| `Program.cs` | Add hosted service + workflow DI | Add channel DI registrations | Low — different DI sections |
| `FleetHubDbContext.cs` | Add WorkflowRunRecord + WorkflowDispatchRecord DbSets | Add DevSmsMessageRecord + DeliveryAttemptRecord DbSets | Low — additive DbSet declarations |
| `appsettings.Development.json` | Add DevWorkflow config section | Add DevNotificationDelivery config section | Low — different JSON keys |
| `DataSeeder.cs` | Add DevPartsOrderEvents seed data | None | None |
| `NotificationDispatchService.cs` | None | Add optional IEmailChannel?/ISmsChannel? params | None |

---

## Phase 5 — Integration Validation

| Agent | Scope | Branch | Status | Notes |
|-------|-------|--------|--------|-------|
| A10-Validate | Full build + test, cross-design smoke, 00-index.md update | `main` | Queued | Run `dotnet build` and `dotnet test`. Verify all 9 designs compile together. Run E2E smoke if available. Update 00-index.md status from "Draft" to "Implemented" for designs 14–22. Fix any merge conflicts or integration issues. |

---

## Build & Test Commands
```bash
# Build entire solution
dotnet build Ironvale.sln

# Run all integration tests
dotnet test tests/backend/IronvaleFleetHub.Api.IntegrationTests/ --configuration Release

# Run frontend build (development mode — avoids Google Fonts fetch failure)
cd src/frontend && npx ng build --configuration=development
```

## Git Conventions
- Branch: `feature/design-{N}-{short-name}`
- Commit: `feat(design-{N}): {description}`
- Trailer: `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`
- Merge strategy: squash merge per feature branch

---

*Last updated: 2026-04-03T05:47Z*
*Phase: Pre-implementation (all agents Queued)*
