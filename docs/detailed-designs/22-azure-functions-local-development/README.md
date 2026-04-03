# Azure Functions Local Development — Detailed Design

## 1. Overview

**Source:** `docs/local-development-strategy.md` Section 3.2 identifies Azure Functions Core Tools as an available local emulator for Azure Functions. The telemetry ingestion pipeline (`src/backend/IronvaleFleetHub.Functions/`) currently targets Azure Functions but has no documented local development workflow.

The production architecture uses an Azure Function (`TelemetryIngestionFunction`) as the sole telemetry write path: it accepts HTTP POST requests with API key validation, inserts telemetry events via Dapper, evaluates retry policies with exponential backoff, and dead-letters failed events. Designs 19, 20, and 21 cover local replacements for Logic Apps, Communication Services, and Azure OpenAI respectively, but none address how to run and test the telemetry ingestion function locally.

**Scope:** document the local development workflow for the Azure Functions project, covering Functions Core Tools configuration, Azurite storage emulation, local database connectivity, API key handling, dead-letter testing, and integration with the main API running in development mode.

**Goals:**

- Enable developers to run the telemetry ingestion function locally with `func start`
- Provide zero-friction defaults that work out of the box alongside the main API
- Support testing of retry, dead-letter, and validation behavior without Azure
- Integrate cleanly with the existing in-memory database and DataSeeder data

**References:**

- [Local Development Strategy](../../local-development-strategy.md)
- [ADR-0001 Azure OpenAI AI-Powered Features](../../adr/integration/0001-azure-openai-ai-powered-features.md) — batch prediction trigger
- [Feature 05 Telemetry & Monitoring](../05-telemetry-monitoring/README.md)
- [Design 10 Telemetry Ingestion Redesign](../10-telemetry-ingestion-redesign/README.md)
- [Design 14 Telemetry Alert Pipeline Unification](../14-telemetry-alert-pipeline-unification/README.md)

## 2. Architecture

### 2.1 Runtime Topology

In local development, two processes run side by side:

| Process | Port | Purpose |
|---------|------|---------|
| ASP.NET Core API (`dotnet run`) | 5000 / 5001 | Main application with DevAuthHandler and in-memory DB |
| Azure Functions Host (`func start`) | 7071 | Telemetry ingestion with Functions Core Tools |

Both processes share the same SQL Server instance when one is configured, or operate independently when the API uses its in-memory database. Section 4.2 describes the database connectivity options in detail.

### 2.2 Storage Emulation

Azure Functions requires a storage backend for internal bookkeeping (trigger state, leases, queues). Locally this is provided by **Azurite**, the official Azure Storage emulator.

Two startup modes are available:

- **Docker:** `docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite`
- **npm:** `npx azurite --silent --location .azurite --debug .azurite/debug.log`

The existing `local.settings.json` already points to Azurite via `"AzureWebJobsStorage": "UseDevelopmentStorage=true"`.

### 2.3 Canonical Ingestion Flow (Local)

1. The Angular frontend or a test client sends `POST http://localhost:7071/api/telemetry` with an `X-Api-Key` header.
2. `TelemetryIngestionFunction` validates the API key against the configured value.
3. The request is deserialized and validated (non-empty `EquipmentId`, non-empty `EventType`).
4. `DapperTelemetryRepository` inserts the event into the local database.
5. On transient failure, exponential backoff retries up to 3 times (delays: 1 s, 4 s, 16 s).
6. On exhaustion, `DeadLetterService` records the failed event for later inspection.
7. The API's `AlertEvaluatorService` processes new telemetry asynchronously if both processes share a database.

### 2.4 Component Diagram

```
┌─────────────────────────────────────────────────────┐
│  Developer Machine                                  │
│                                                     │
│  ┌──────────────┐   POST /api/telemetry   ┌──────────────────┐
│  │ Angular App  │ ──────────────────────► │ Functions Host    │
│  │ (ng serve)   │                         │ (func start)     │
│  │ :4200        │                         │ :7071            │
│  └──────────────┘                         │                  │
│                                           │  TelemetryIngest │
│  ┌──────────────┐                         │  Function        │
│  │ ASP.NET API  │                         └────────┬─────────┘
│  │ (dotnet run) │                                  │
│  │ :5000        │                                  │
│  └──────┬───────┘                                  │
│         │                                          │
│         ▼                                          ▼
│  ┌──────────────┐                         ┌──────────────────┐
│  │ SQL Server   │ ◄────────────────────── │ Dapper Repo      │
│  │ (localdb or  │                         │ (bulk insert)    │
│  │  Docker)     │                         └──────────────────┘
│  └──────────────┘
│         ▲
│         │
│  ┌──────────────┐
│  │ Azurite      │  (Functions storage backend)
│  │ :10000-10002 │
│  └──────────────┘
└─────────────────────────────────────────────────────┘
```

## 3. Components, Types, and Configuration

### 3.1 `local.settings.json` — Development Defaults

The existing file at `src/backend/IronvaleFleetHub.Functions/local.settings.json` needs the following values populated for local development:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Telemetry__ApiKey": "dev-api-key-12345",
    "ConnectionStrings__DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=IronvaleFleetHub_Dev;Trusted_Connection=True;"
  }
}
```

| Key | Value | Purpose |
|-----|-------|---------|
| `AzureWebJobsStorage` | `UseDevelopmentStorage=true` | Routes storage operations to Azurite |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` | .NET isolated worker model |
| `Telemetry__ApiKey` | `dev-api-key-12345` | Deterministic dev key for testing (non-secret) |
| `ConnectionStrings__DefaultConnection` | LocalDB or Docker SQL connection string | Points to a real SQL instance for Dapper queries |

> **Convention note:** `local.settings.json` is excluded from source control (`.gitignore`). A `local.settings.example.json` should be committed with placeholder values and comments so developers can copy and configure it.

### 3.2 `TelemetryIngestionFunction` — Existing Function

**File:** `src/backend/IronvaleFleetHub.Functions/Functions/TelemetryIngestionFunction.cs`

The function already works locally when its dependencies are satisfied. No code changes are required. The local development concern is purely configuration:

- **API key:** the function reads `_configuration["Telemetry:ApiKey"]`. When this is empty or unset, the function returns `401 Unauthorized` for all requests. Developers must set `Telemetry__ApiKey` in `local.settings.json`.
- **Database:** `DapperTelemetryRepository` executes raw SQL via `IConfiguration.GetConnectionString("DefaultConnection")`. This requires a real SQL Server instance (LocalDB, Docker, or remote).

### 3.3 `DapperTelemetryRepository` — Database Dependency

**File:** `src/backend/IronvaleFleetHub.Functions/Services/DapperTelemetryRepository.cs`

This repository uses Dapper for bulk SQL inserts and cannot use the EF Core in-memory provider. Local development therefore requires one of:

| Option | Setup | Pros | Cons |
|--------|-------|------|------|
| **LocalDB** | Installed with Visual Studio | Zero Docker, fast | Windows only |
| **Docker SQL Server** | `docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Dev@12345" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest` | Cross-platform | Requires Docker |
| **Shared remote SQL** | Connection string to a dev Azure SQL instance | No local setup | Requires network, costs |

The recommended default is **LocalDB** for Windows developers and **Docker SQL Server** for macOS/Linux.

### 3.4 `DeadLetterService` — Failure Recording

**File:** `src/backend/IronvaleFleetHub.Functions/Services/DeadLetterService.cs`

The dead-letter service writes `TelemetryDeadLetterEntry` records to the same database. In local development, this means failed ingestion attempts are visible in the database for inspection.

Developers can query dead-letter entries directly:

```sql
SELECT * FROM TelemetryDeadLetters ORDER BY CreatedAt DESC;
```

### 3.5 `DevTelemetrySeeder` — Proposed New Component

**Type:** optional startup utility for the Functions project
**Responsibility:** ensures the Functions database has the same baseline data as the API's `DataSeeder`

When both the API (in-memory DB) and the Functions host (SQL Server) are running, the Functions database needs:

- **Organizations:** `a1b2c3d4-0001-*` (Northern Construction Ltd.) and `a1b2c3d4-0002-*` (Pacific Mining Corp.)
- **Equipment:** the 10 seeded equipment items (`c1b2c3d4-0001` through `c1b2c3d4-0010`) so that `EquipmentId` validation passes
- **Alert Thresholds:** the 5 model thresholds so that `AlertEvaluatorService` (if shared) can evaluate events

This seeder should run on Functions host startup in development only and be idempotent (skip if data already exists).

> **Alternative approach:** if both processes share a single SQL Server database, the API's `DataSeeder` handles all seed data and the Functions project needs no separate seeder.

### 3.6 `local.settings.example.json` — Committed Template

**Proposed new file:** `src/backend/IronvaleFleetHub.Functions/local.settings.example.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Telemetry__ApiKey": "dev-api-key-12345",
    "ConnectionStrings__DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=IronvaleFleetHub_Dev;Trusted_Connection=True;"
  },
  "_comments": {
    "Telemetry__ApiKey": "Use any non-empty string. Callers must send the same value in the X-Api-Key header.",
    "ConnectionStrings__DefaultConnection": "Point to LocalDB, Docker SQL Server, or a shared dev instance. Dapper requires a real SQL Server."
  }
}
```

## 4. Detailed Behavior

### 4.1 Startup Sequence

1. Start Azurite (if not already running): `npx azurite --silent --location .azurite`
2. Start the Functions host: `cd src/backend/IronvaleFleetHub.Functions && func start`
3. The Functions host loads `local.settings.json` and binds configuration.
4. The HTTP endpoint becomes available at `http://localhost:7071/api/telemetry`.

If Azurite is not running, the Functions host fails at startup with a storage connection error. The error message should be documented in the project README.

### 4.2 Database Connectivity Options

**Option A: Shared SQL Server (Recommended)**

Both the API and Functions host connect to the same SQL Server instance. The API runs EF Core migrations (or `DataSeeder` populates tables), and the Functions host writes telemetry into the same database.

- Set `ConnectionStrings:DefaultConnection` in both `appsettings.Development.json` and `local.settings.json` to the same connection string.
- The API must NOT use in-memory database in this mode (remove the empty-string fallback or provide the connection string).
- `AlertEvaluatorService` in the API can evaluate newly ingested telemetry because both processes see the same data.

**Option B: Independent Databases**

The API uses its in-memory database and the Functions host uses a separate SQL Server. Telemetry ingested through the function is not visible to the API.

- This mode is simpler to start (no shared SQL setup required).
- Useful when developing features that don't depend on telemetry flow (equipment management, parts ordering, etc.).
- The Functions project needs its own seed data via `DevTelemetrySeeder`.

**Option C: API Bypass Mode**

For developers who only need the API and don't require the telemetry pipeline, the function is not started at all. The frontend can be configured to skip telemetry calls, or the API can expose a development-only direct-ingest endpoint that writes telemetry to the in-memory database without going through the Function.

### 4.3 API Key Handling

The function validates `X-Api-Key` against `Telemetry:ApiKey` from configuration. In local development:

- `local.settings.json` sets `Telemetry__ApiKey` to `dev-api-key-12345` (a well-known development value).
- Callers (Angular app, curl, integration tests) send `X-Api-Key: dev-api-key-12345`.
- The Angular app's environment configuration should include the dev API key for local mode.

> **Security note:** the dev API key must never be used in production. The value `dev-api-key-12345` is intentionally obviously fake. Production keys are managed through Azure Key Vault and are not committed to source control.

### 4.4 Retry and Dead-Letter Testing

To test retry behavior locally, developers can:

1. **Simulate transient failure:** temporarily stop the SQL Server container while the function is running, then send a telemetry request. The function retries 3 times with exponential backoff (1 s, 4 s, 16 s), then dead-letters the event.
2. **Inspect dead-letter entries:** query the `TelemetryDeadLetters` table in the local database, or add a dev-only endpoint in the Functions project to list recent dead-letter entries.
3. **Verify idempotency:** send the same telemetry event twice and confirm only one record is created (if idempotency is implemented) or two records exist (current behavior — the function does not deduplicate).

### 4.5 Integration with AlertEvaluatorService

When using Option A (shared SQL Server), telemetry events ingested through the Function are available for alert evaluation. However, `AlertEvaluatorService` is called synchronously within the API's request pipeline, not triggered by new database rows.

**Current gap:** there is no event-driven trigger connecting Function-ingested telemetry to `AlertEvaluatorService`. Options for local development:

1. **Manual evaluation:** call the alert evaluation endpoint after ingesting telemetry.
2. **Polling hosted service:** a dev-only `BackgroundService` in the API that periodically checks for un-evaluated telemetry events and runs `AlertEvaluatorService`.
3. **Accept the gap:** in production, Logic Apps or an event-driven pipeline handles this. Locally, alert evaluation happens only through the API's own telemetry endpoints.

The recommended approach for v1 is option 3 (accept the gap) and document it clearly. Design #19 (Logic Apps Local Workflow Engine) may later introduce a workflow that bridges this.

### 4.6 Frontend Integration

The Angular app needs to know where to send telemetry in each environment:

| Environment | Telemetry Endpoint | API Key Source |
|-------------|-------------------|----------------|
| Production | `https://<function-app>.azurewebsites.net/api/telemetry` | Azure Key Vault |
| Local Dev | `http://localhost:7071/api/telemetry` | `environment.ts` |
| Local (no function) | `http://localhost:5000/api/v1/telemetry/ingest` (if bypass endpoint exists) | Not required |

The Angular `environment.development.ts` should include:

```typescript
export const environment = {
  production: false,
  apiBaseUrl: '/api/v1',
  telemetryEndpoint: 'http://localhost:7071/api/telemetry',
  telemetryApiKey: 'dev-api-key-12345',
};
```

## 5. Acceptance Tests

### 5.1 Basic Ingestion

Given the Functions host is running with Azurite and a local SQL Server, when `POST /api/telemetry` is called with a valid `X-Api-Key` and telemetry payload for equipment `c1b2c3d4-0001-0000-0000-000000000001` (CAT 320 Excavator #1), then the response is `202 Accepted` and the telemetry event is visible in the database.

**How to verify:** query `SELECT TOP 1 * FROM TelemetryEvents ORDER BY Timestamp DESC` in the local database and confirm the equipment ID and sensor values match the request.

### 5.2 API Key Rejection

Given the Functions host is running, when a request is sent without an `X-Api-Key` header or with an incorrect key, then the response is `401 Unauthorized`.

**How to verify:** `curl -X POST http://localhost:7071/api/telemetry -H "Content-Type: application/json" -d '{"equipmentId":"..."}' -w "%{http_code}"` returns `401`.

### 5.3 Validation Failure

Given a valid API key, when a request is sent with `equipmentId` set to `00000000-0000-0000-0000-000000000000` (empty GUID), then the response is `400 Bad Request`.

### 5.4 Dead-Letter on Persistent Failure

Given the database is unreachable (SQL Server stopped), when a telemetry request is sent, then after 3 retries the event is dead-lettered and the response is `500 Internal Server Error`. When the database is restored, the dead-letter entry is queryable.

**How to verify:** stop the SQL Server container, send a request, restart the container, then query `SELECT * FROM TelemetryDeadLetters`.

### 5.5 Azurite Dependency

Given Azurite is not running, when `func start` is executed, then the Functions host fails with a clear storage connection error and does not silently start without storage.

## 6. Security Considerations

- `local.settings.json` must remain in `.gitignore` to prevent accidental commit of connection strings.
- `local.settings.example.json` must use only development-safe placeholder values.
- The dev API key (`dev-api-key-12345`) must never appear in production configuration or CI/CD pipelines.
- The Functions host should bind to `localhost` only in local development to prevent LAN exposure.
- SQL Server containers should use strong-enough passwords even locally to prevent accidental habits of weak credentials.

## 7. Multi-Tenancy Considerations

The `TelemetryIngestionFunction` writes telemetry events with an `OrganizationId` field (resolved from the equipment lookup). In local development:

- Equipment must exist in the database with correct `OrganizationId` values for ingestion to succeed.
- When using the shared SQL Server approach (Option A), the API's global query filters still apply — telemetry for Org2 equipment is not visible to a user authenticated as Org1 through `DevAuthHandler`.
- When using `DevTelemetrySeeder`, seed data must include equipment from both organizations to test cross-tenant isolation.

The existing `DataSeeder` seeds equipment for both `Northern Construction Ltd.` (Org1) and `Pacific Mining Corp.` (Org2), providing adequate multi-tenant test coverage.

## 8. Open Questions

1. Should the Functions project include a dev-only HTTP endpoint to list recent dead-letter entries, or is direct SQL querying sufficient?
2. Should the API expose a development-only bypass endpoint (`POST /api/dev/telemetry/ingest`) that writes directly to the in-memory database, avoiding the need to run the Functions host at all?
3. Should the EF Core migrations be shared between the API and Functions projects, or should the Functions project manage its own schema via Dapper SQL scripts?
4. Should the `DevTelemetrySeeder` reuse the API's `DataSeeder` logic (via a shared library), or maintain its own minimal seed set?
