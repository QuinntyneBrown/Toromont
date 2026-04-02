# Local Development Strategy

**Date:** 2026-04-02
**Status:** Proposed
**Audience:** Backend Engineers, Frontend Engineers, DevOps

---

## 1. Overview

This document defines the strategy for running the Toromont Fleet Hub application entirely on a developer's local machine **without requiring access to Azure cloud services**. The goal is to ensure every engineer can build, run, and test all application features using only local tools, emulators, and stubs.

### 1.1 Objectives

- **Zero cloud dependency** for the inner development loop (code → build → run → test)
- **Feature parity** with the cloud environment wherever possible
- **Fast feedback** with minimal setup overhead
- **Offline-capable** development for environments with restricted network access

### 1.2 Scope

The strategy covers local replacements for every Azure service referenced in the architecture:

| # | Azure Service | Purpose in Fleet Hub |
|---|---------------|----------------------|
| 1 | Azure SQL Database | Relational data store |
| 2 | Microsoft Entra ID (Azure AD) | Authentication & identity |
| 3 | Azure Application Insights | APM, logging, telemetry |
| 4 | Azure SignalR Service | Real-time notifications |
| 5 | Azure Functions | Serverless telemetry ingestion & batch AI jobs |
| 6 | Azure Logic Apps | Workflow automation (reminders, escalations) |
| 7 | Azure Communication Services | Email & SMS delivery |
| 8 | Azure OpenAI Service | Predictive maintenance & anomaly detection |
| 9 | Azure API Management | API gateway & rate limiting |
| 10 | Azure Key Vault | Secrets management |

---

## 2. Current Local Development Support

The codebase already includes several mechanisms that enable local development without cloud access. These are controlled through `appsettings.Development.json` and conditional logic in `Program.cs`.

### 2.1 Database — EF Core In-Memory Provider ✅

**Status:** Already implemented

When running in the `Development` environment with an empty connection string, the API automatically falls back to EF Core's in-memory database provider:

```csharp
// Program.cs – lines 34-53
if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<FleetHubDbContext>((sp, options) =>
    {
        options.UseInMemoryDatabase("FleetHubDev");
    });
}
```

**Limitations:**
- No SQL Server–specific features (e.g., full-text search, JSON columns, temporal tables)
- No schema migration validation
- Data is lost on every restart

**Upgrade path (optional):** For higher-fidelity testing, developers can run SQL Server 2022 locally via Docker (see Section 3.1).

### 2.2 Authentication — Dev Auth Handler ✅

**Status:** Already implemented

When `Authentication:UseDevMode` is `true` (the default in `appsettings.Development.json`), a custom `DevAuthHandler` bypasses Azure AD and issues synthetic claims:

```csharp
// Program.cs – lines 64-84
if (useDevAuth)
{
    builder.Services.AddAuthentication("DevScheme")
        .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevScheme", null);
}
```

This enables multi-tenant and role-based testing without Azure AD app registrations or network access. The frontend MSAL configuration is bypassed when the backend operates in dev mode.

### 2.3 Application Insights — Console Logging Fallback ✅

**Status:** Already implemented

When the `ApplicationInsights:ConnectionString` is empty (as in the development config), Serilog writes exclusively to the console:

```csharp
// Program.cs – lines 16-32
loggerConfig.WriteTo.Console();
// Application Insights sink is only added when connection string is non-empty
```

No telemetry data is sent to Azure. Developers see structured log output in the terminal.

### 2.4 SignalR — In-Process Hub ✅

**Status:** Already implemented

The ASP.NET Core built-in SignalR server runs in-process during development. The `NotificationHub` at `/hubs/notifications` operates without the Azure SignalR Service. The frontend proxy configuration already routes WebSocket connections to the local backend:

```json
// proxy.conf.json
{
  "/hubs": {
    "target": "http://localhost:5000",
    "secure": false,
    "ws": true
  }
}
```

No additional setup is required for real-time notification testing.

---

## 3. Emulators & Local Tools Available

The following Azure services have **official or well-established emulators** that can run locally.

### 3.1 SQL Server — Docker Container

**Emulator available:** Yes (official Microsoft image)

For scenarios where the in-memory provider is insufficient (migration testing, SQL-specific features), developers can run SQL Server 2022 in Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Dev@12345" \
  -p 1433:1433 --name sqlserver-dev \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=FleetHubDev;User Id=sa;Password=Dev@12345;TrustServerCertificate=True"
  }
}
```

Then apply EF Core migrations:

```bash
cd src/backend/ToromontFleetHub.Api
dotnet ef database update
```

### 3.2 Azure Functions — Azure Functions Core Tools

**Emulator available:** Yes (official Microsoft CLI tool)

[Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local) allows running Azure Functions locally without deploying to the cloud.

**Installation:**

```bash
# macOS
brew tap azure/functions && brew install azure-functions-core-tools@4

# Windows
winget install Microsoft.Azure.FunctionsCoreTools

# Linux (Ubuntu/Debian)
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
sudo apt-get update && sudo apt-get install azure-functions-core-tools-4
```

**Usage:** When function projects are added to the repository, they can be run with:

```bash
cd src/functions
func start
```

The Functions runtime supports HTTP triggers, timer triggers, and queue triggers locally. Telemetry ingestion and predictive maintenance batch jobs (ADR Infrastructure/0002) can be developed and tested entirely offline.

**Local storage dependency:** Azure Functions Core Tools requires a storage backend for triggers and bindings. Use **Azurite** (see Section 3.3) to provide this locally.

### 3.3 Azure Storage — Azurite Emulator

**Emulator available:** Yes (official Microsoft emulator)

[Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) emulates Azure Blob Storage, Queue Storage, and Table Storage. It is required as a backing store for Azure Functions Core Tools.

```bash
# Install globally
npm install -g azurite

# Run
azurite --silent --location /tmp/azurite --debug /tmp/azurite/debug.log
```

Or via Docker:

```bash
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite
```

Connection string for local development:

```
UseDevelopmentStorage=true
```

### 3.4 Azure Key Vault — .NET User Secrets

**Emulator needed:** No

For local development, .NET User Secrets replaces Azure Key Vault:

```bash
cd src/backend/ToromontFleetHub.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;..."
```

Secrets are stored in the developer's home directory (`~/.microsoft/usersecrets/`) and are never committed to source control. The existing `appsettings.Development.json` with empty values also works for a zero-configuration experience.

### 3.5 Azure API Management — Not Required Locally

**Emulator needed:** No

API Management provides gateway-level concerns (rate limiting, security headers, analytics) that are not required for local development. The application already applies essential security headers via middleware in `Program.cs` (lines 162–169). Rate limiting can be tested with ASP.NET Core's built-in `RateLimiter` middleware if needed.

---

## 4. Services Requiring Local Stubs

The following Azure services **do not have official emulators** and require stub implementations or alternative tools for local development.

### 4.1 Azure Logic Apps — Local Stub Required

**Official emulator:** Partial — [Azure Logic Apps Standard single-tenant runtime](https://learn.microsoft.com/en-us/azure/logic-apps/create-single-tenant-workflows-visual-studio-code) can run locally in VS Code, but requires Azure Storage emulation (Azurite) and has limited connector support.

**Recommended local strategy:**

| Approach | Effort | Fidelity |
|----------|--------|----------|
| **Option A: Ignore in local dev** — Workflows (service reminders, escalations) are background processes that don't affect core UI/API functionality. Skip them locally. | None | Low |
| **Option B: In-process hosted service** — Create a .NET `IHostedService` that replaces the Logic Apps workflows with simple timer-based logic for reminders and escalations. | Medium | High |
| **Option C: Local Logic Apps runtime** — Use the single-tenant Logic Apps runtime in VS Code with Azurite for storage. | Medium | Highest |

**Recommendation:** Start with **Option A** for most development tasks. Implement **Option B** when actively working on notification or escalation features. The hosted service should be conditionally registered only in the `Development` environment.

### 4.2 Azure Communication Services — Email/SMS Stubs

**Official emulator:** No

**Recommended local strategy:**

For **email**, use one of these local SMTP tools to capture outgoing messages:

| Tool | Description | Installation |
|------|-------------|-------------|
| [MailHog](https://github.com/mailhog/MailHog) | SMTP server with web UI for viewing captured emails | `docker run -p 1025:1025 -p 8025:8025 mailhog/mailhog` |
| [smtp4dev](https://github.com/rnwood/smtp4dev) | .NET-based SMTP server with web UI | `dotnet tool install -g Rnwood.Smtp4dev && smtp4dev` |
| [Papercut SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP) | Windows-native SMTP testing tool | Windows installer available |

For **SMS**, no delivery is needed locally. Log SMS messages to the console:

```csharp
public class LocalSmsService : ISmsService
{
    private readonly ILogger<LocalSmsService> _logger;

    public Task SendAsync(string phoneNumber, string message)
    {
        _logger.LogInformation("SMS to {Phone}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
```

**Stub to develop:** Create an `INotificationChannel` abstraction with `AzureCommunicationEmailChannel` (production) and `LocalSmtpEmailChannel` (development) implementations. Similarly, create `AzureSmsSender` and `ConsoleSmsStub` implementations. Register the appropriate implementation based on the environment.

### 4.3 Azure OpenAI Service — Mock or Local Model

**Official emulator:** No

**Recommended local strategy:**

| Approach | Effort | Fidelity | Notes |
|----------|--------|----------|-------|
| **Option A: Mock responses** — Return canned predictions and anomaly scores from a static data set. | Low | Low | Good for UI development |
| **Option B: Local LLM (Ollama)** — Run an open-source model locally via [Ollama](https://ollama.com/). | Medium | Medium | Requires 8+ GB RAM; models like Llama 3, Mistral, or Phi-3 can approximate OpenAI APIs |
| **Option C: OpenAI-compatible API** — Use the [Azure OpenAI Proxy](https://github.com/microsoft/azure-openai-proxy) or directly call the OpenAI public API with a personal key. | Low | High | Requires internet and a personal API key |

**Recommendation:** Use **Option A** (mock responses) by default. Create an `IAiInsightsService` interface with:

- `AzureOpenAiInsightsService` — production implementation calling Azure OpenAI
- `MockAiInsightsService` — development implementation returning seeded predictions

```csharp
public class MockAiInsightsService : IAiInsightsService
{
    public Task<MaintenancePrediction> PredictMaintenanceAsync(Guid equipmentId)
    {
        return Task.FromResult(new MaintenancePrediction
        {
            EquipmentId = equipmentId,
            ConfidenceScore = 0.85,
            PredictedIssue = "Hydraulic system pressure decline",
            RecommendedAction = "Schedule hydraulic fluid replacement within 14 days",
            Priority = "High"
        });
    }
}
```

This approach allows all UI screens and API endpoints to function without cloud access. Developers working on the AI module can opt into Option B or C.

---

## 5. Gap Analysis Summary

| Azure Service | Local Alternative | Available Today | Custom Development Needed |
|---------------|-------------------|:---:|:---:|
| Azure SQL Database | EF Core In-Memory / Docker SQL Server | ✅ | No |
| Microsoft Entra ID | DevAuthHandler | ✅ | No |
| Application Insights | Console logging (Serilog) | ✅ | No |
| Azure SignalR Service | In-process SignalR | ✅ | No |
| Azure Functions | Azure Functions Core Tools | ✅ | No |
| Azure Storage (Functions backing) | Azurite emulator | ✅ | No |
| Azure Key Vault | .NET User Secrets | ✅ | No |
| Azure API Management | Not needed locally | ✅ | No |
| Azure Logic Apps | `IHostedService` stub | ❌ | **Yes — Medium effort** |
| Azure Communication Services | MailHog + Console SMS logger | ❌ | **Yes — Low effort** |
| Azure OpenAI Service | Mock AI service | ❌ | **Yes — Low effort** |

**Items requiring custom development:**

1. **`IHostedService` for Logic Apps workflows** — A background service that replicates reminder and escalation timer logic. Estimated effort: 1–2 days.
2. **`INotificationChannel` abstraction** — Interface + local SMTP and console SMS implementations. Estimated effort: 0.5–1 day.
3. **`IAiInsightsService` mock** — Interface + mock implementation with canned predictions. Estimated effort: 0.5–1 day.

Total estimated effort for full local parity: **~2–4 days of engineering work**.

---

## 6. Recommended Local Setup

### 6.1 Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0+ | Backend build & run |
| [Node.js](https://nodejs.org/) | 20 LTS+ | Frontend build & run |
| [Angular CLI](https://angular.dev/) | 17+ | `ng serve` |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest | SQL Server, MailHog, Azurite |
| [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local) | v4 | Local Functions runtime (when function projects are added) |

### 6.2 Quick Start (Minimal — No Docker)

```bash
# 1. Clone the repository
git clone https://github.com/QuinntyneBrown/Toromont.git
cd Toromont

# 2. Start the backend (uses in-memory DB + dev auth)
cd src/backend/ToromontFleetHub.Api
dotnet run

# 3. In a separate terminal, start the frontend
cd src/frontend
npm install
npx ng serve
```

The application is available at `http://localhost:4200`. The API runs at `http://localhost:5000`. Authentication is handled by `DevAuthHandler` with synthetic claims — no Azure AD configuration needed.

### 6.3 Full Local Stack (Docker)

For higher-fidelity local development, use Docker to run supporting services:

```bash
# SQL Server 2022
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Dev@12345" \
  -p 1433:1433 --name sqlserver-dev -d mcr.microsoft.com/mssql/server:2022-latest

# MailHog (email capture)
docker run -p 1025:1025 -p 8025:8025 --name mailhog-dev -d mailhog/mailhog

# Azurite (Azure Storage emulator — needed for Functions)
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  --name azurite-dev -d mcr.microsoft.com/azure-storage/azurite
```

Update `appsettings.Development.json` to point to the local SQL Server:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=FleetHubDev;User Id=sa;Password=Dev@12345;TrustServerCertificate=True"
  }
}
```

MailHog web UI is available at `http://localhost:8025` for inspecting captured emails.

---

## 7. Development Workflow

```
┌─────────────────────────────────────────────┐
│              Developer Machine               │
│                                              │
│  ┌──────────┐    proxy     ┌──────────────┐ │
│  │ Angular  │ ──────────►  │  ASP.NET     │ │
│  │ Dev      │  /api, /hubs │  Core API    │ │
│  │ Server   │              │              │ │
│  │ :4200    │  ◄────────── │  :5000       │ │
│  └──────────┘   SignalR    │              │ │
│                            │  ┌─────────┐ │ │
│                            │  │DevAuth  │ │ │
│                            │  │Handler  │ │ │
│                            │  └─────────┘ │ │
│                            │  ┌─────────┐ │ │
│                            │  │In-Memory│ │ │
│                            │  │Database │ │ │
│                            │  └─────────┘ │ │
│                            └──────────────┘ │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │         Optional Docker Services        │  │
│  │                                        │  │
│  │  SQL Server :1433  │  MailHog :8025    │  │
│  │  Azurite :10000    │  (email capture)  │  │
│  └────────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

### 7.1 Inner Development Loop

1. **Code** — Edit backend or frontend source files
2. **Build** — `dotnet run` (backend, hot-reload) / `ng serve` (frontend, HMR)
3. **Test** — Run unit tests (`dotnet test`, `ng test`) or E2E tests (`npx playwright test`)
4. **Verify** — Check API via Swagger UI at `http://localhost:5000/swagger`

### 7.2 Switching to Cloud-Connected Mode

To test against real Azure services, update `appsettings.Development.json`:

```json
{
  "Authentication": {
    "UseDevMode": false
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>"
  },
  "ConnectionStrings": {
    "DefaultConnection": "<azure-sql-connection-string>"
  }
}
```

This allows testing Azure AD authentication flows and production database schemas without redeploying.

---

## 8. Future Recommendations

1. **Add a `docker-compose.yml`** — Define the full local stack (SQL Server, MailHog, Azurite) in a single file for one-command startup: `docker compose up -d`.

2. **Seed data on startup** — Extend the `FleetHubDbContext` or add a `DbSeeder` service to populate the in-memory (or local SQL Server) database with realistic test data on startup, including sample equipment, telemetry readings, work orders, and user accounts.

3. **Create service abstractions** — Introduce interfaces (`INotificationChannel`, `IAiInsightsService`, `IWorkflowEngine`) that enable clean swapping between Azure-backed and local implementations via dependency injection.

4. **Add a developer configuration guide** — Document environment-specific settings in a `CONTRIBUTING.md` or `docs/setup.md` file so new engineers can onboard quickly.

5. **Evaluate Dev Containers** — Consider a [VS Code Dev Container](https://code.visualstudio.com/docs/devcontainers/containers) or [GitHub Codespaces](https://github.com/features/codespaces) configuration (`.devcontainer/devcontainer.json`) that pre-installs all tools and starts Docker services automatically.

---

## 9. References

- [ADR Infrastructure/0001 — Azure Cloud Platform](adr/infrastructure/0001-azure-cloud-platform.md)
- [ADR Infrastructure/0002 — Azure Functions Telemetry Ingestion](adr/infrastructure/0002-azure-functions-telemetry-ingestion.md)
- [ADR Infrastructure/0003 — Azure Logic Apps Workflow Automation](adr/infrastructure/0003-azure-logic-apps-workflow-automation.md)
- [ADR Infrastructure/0004 — SignalR Real-Time Notifications](adr/infrastructure/0004-signalr-real-time-notifications.md)
- [ADR Infrastructure/0005 — Azure API Management Gateway](adr/infrastructure/0005-azure-api-management-gateway.md)
- [ADR Integration/0001 — Azure OpenAI AI-Powered Features](adr/integration/0001-azure-openai-ai-powered-features.md)
- [ADR Integration/0002 — Azure Communication Services Notifications](adr/integration/0002-azure-communication-services-notifications.md)
- [ADR Security/0001 — Microsoft Entra ID Authentication](adr/security/0001-microsoft-entra-id-authentication.md)
- [Azure Functions Core Tools documentation](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azurite Storage Emulator documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)
