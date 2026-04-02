# Azure Deployment Strategy

**Date:** 2026-04-02
**Status:** Proposed
**Audience:** Engineering, DevOps, Platform Owners

---

## 1. Executive Summary

This document defines the recommended Azure deployment strategy for Toromont Fleet Hub across **dev**, **staging**, and **production**, with **GitHub Actions** as the CI/CD platform.

### 1.1 Recommended Strategy

Use the **cheapest architecture that still fits the current codebase**:

- Host the **Angular frontend and ASP.NET Core API together in one Azure App Service app**
- Use a **separate low-cost dev App Service**
- Use **one live App Service for staging + production** with a **staging deployment slot**
- Use **Azure SQL Database serverless** for dev and staging to minimize idle cost
- Use **Azure SQL Database provisioned General Purpose** for production unless the workload proves extremely light
- Use **build once, promote the same artifact** from dev to staging to production
- Use **GitHub OIDC** for Azure authentication, not publish profiles or long-lived secrets

### 1.2 Why This Is the Best Fit for This Repo

The current frontend calls:

- `/api/*`
- `/hubs/notifications`

Those are **same-origin relative paths**, and the app uses **SignalR/WebSockets**. Splitting frontend and backend across separate Azure services would require:

- environment-specific API base URLs, or
- an additional reverse proxy layer such as Front Door or API Management

That increases both **cost** and **operational complexity**.

The cheapest clean option is therefore:

1. Build Angular
2. Copy the built SPA into the ASP.NET Core app's `wwwroot`
3. Deploy one combined package to App Service

This keeps `/api` and `/hubs/notifications` working without extra infrastructure.

---

## 2. Target Topology

## 2.1 Environment Layout

| Environment | Hosting | Database | Notes |
|---|---|---|---|
| `dev` | Separate Linux App Service on **Free F1** initially | Azure SQL Database **General Purpose serverless** with auto-pause | Cheapest shared Azure dev environment |
| `staging` | `staging` slot on the live Linux App Service | Separate staging Azure SQL DB, also **serverless** | No extra slot charge; good for promotion testing |
| `prod` | Production slot on the live Linux App Service | Separate prod Azure SQL DB, **provisioned General Purpose** | Predictable performance and no auto-pause cold starts |

## 2.2 App Service Design

### Dev

- App Service Plan: **Linux Free F1**
- App Service: `app-fleethub-dev`
- Custom domain: **not required**
- SSL: use default `azurewebsites.net`

**Upgrade trigger:** move dev from `F1` to `B1` if any of the following happens:

- the shared dev environment is used heavily during the day
- developers hit CPU-minute limits
- WebSocket limits become a problem
- you need a stable always-on shared environment

### Staging and Production

- App Service Plan: **Linux Standard S1**
- App Service: `app-fleethub-live`
- Slot: `staging`

This is the key cost optimization:

- **Standard is the first tier that supports deployment slots**
- **slots do not add a separate app charge**
- one S1 plan with a staging slot is cheaper and simpler than running separate staging and production paid plans

## 2.3 Database Design

### Non-Production

- One logical SQL server for non-prod
- One DB for `dev`
- One DB for `staging`
- Both on **General Purpose serverless**
- Auto-pause enabled for `dev`
- Auto-pause enabled for `staging`, unless the team wants it always warm during active release windows

### Production

- Separate logical SQL server for production
- One DB for `prod`
- Start with **General Purpose provisioned**

Reasoning:

- Microsoft states serverless is better for **variable usage with idle periods**
- Microsoft also states provisioned is generally better for **more uniform and substantial utilization**

Production is usually the least suitable place to depend on auto-pause.

## 2.4 Services Explicitly Deferred

Do **not** add these in v1:

- Azure Static Web Apps
- Azure Front Door
- Azure API Management
- Azure SignalR Service
- Azure Container Registry
- Azure Container Apps

These can be added later if scale, security, or multi-region requirements justify them. They are not needed for the current repo and would increase monthly cost.

---

## 3. Repo Changes Required Before Azure Deployment

The current repo is close, but not deployment-ready for combined hosting.

## 3.1 Backend Must Serve the Angular SPA

The API currently does **not** serve static frontend assets. Add:

- `UseDefaultFiles()`
- `UseStaticFiles()`
- `MapFallbackToFile("index.html")`

The route order should remain:

1. security/middleware
2. `/api` controllers
3. `/hubs/notifications`
4. SPA fallback

## 3.2 CI Must Publish One Combined Artifact

The CI workflow should:

1. build Angular in `src/frontend`
2. copy the Angular output into `src/backend/ToromontFleetHub.Api/wwwroot`
3. run `dotnet publish` for the API
4. zip the published output

That zip becomes the **single release artifact** promoted across all environments.

## 3.3 Stop Treating Azure Dev as "Local Dev"

Local development can keep using:

- in-memory DB fallback
- `Authentication:UseDevMode = true`

Azure `dev`, `staging`, and `prod` should all use:

- real Azure SQL databases
- real Entra ID app registrations
- `Authentication:UseDevMode = false`

## 3.4 Add Real EF Core Migrations

The backend currently has no checked-in EF migrations and no migration execution strategy.

Create:

- a `Migrations` folder in the API project or a dedicated migrations project
- an EF migration bundle in CI/CD

Recommended output artifact:

- `webapp.zip`
- `efbundle` migration bundle
- `release-manifest.json`

The manifest should include:

- commit SHA
- build number
- artifact version
- whether DB changes are included

---

## 4. Azure Resource Model

## 4.1 Resource Groups

Use the following logical split:

- `rg-fleethub-dev`
- `rg-fleethub-live`
- `rg-fleethub-data-nonprod`
- `rg-fleethub-data-prod`
- `rg-fleethub-monitoring`

If the organization prefers fewer resource groups, `dev` can remain isolated while `live`, `data`, and `monitoring` can be consolidated. The above split is cleaner for access control.

## 4.2 Recommended Azure Resources

| Resource | Dev | Staging | Prod |
|---|---|---|---|
| App Service Plan | Yes | Shared with prod | Shared with staging |
| App Service App | Yes | Shared app, `staging` slot | Shared app, production slot |
| Azure SQL Logical Server | Shared non-prod | Shared non-prod | Separate |
| Azure SQL Database | Yes | Yes | Yes |
| Log Analytics Workspace | Shared | Shared | Shared or separate later |
| Application Insights | Yes | Yes | Yes |
| Key Vault | Shared non-prod | Shared non-prod | Separate |

## 4.3 App Settings / Connection Strings

These should be set per environment or per slot:

- `ASPNETCORE_ENVIRONMENT`
- `ConnectionStrings__DefaultConnection`
- `AzureAd__Instance`
- `AzureAd__TenantId`
- `AzureAd__ClientId`
- `Authentication__UseDevMode`
- `ApplicationInsights__ConnectionString`

For the staging slot, mark these as **slot settings**:

- DB connection string
- Entra client IDs if staging uses separate app registrations
- any third-party service endpoints or secrets

That ensures staging values do not move into production during swap.

## 4.4 Identity Model

Use separate Entra app registrations for each deployed environment:

- `fleethub-spa-dev`
- `fleethub-api-dev`
- `fleethub-spa-staging`
- `fleethub-api-staging`
- `fleethub-spa-prod`
- `fleethub-api-prod`

Reasons:

- separate redirect URIs per hostname
- lower blast radius if one environment is misconfigured
- cleaner consent and troubleshooting boundaries

This has no meaningful platform cost and is worth doing from the start.

---

## 5. Bicep Files To Create

Create a new top-level `infra/` folder.

## 5.1 Proposed File Structure

```text
infra/
  main.bicep
  env/
    dev.bicepparam
    staging.bicepparam
    prod.bicepparam
  modules/
    appServicePlan.bicep
    webApp.bicep
    webAppSlot.bicep
    logAnalytics.bicep
    applicationInsights.bicep
    sqlServer.bicep
    sqlDatabase.bicep
    keyVault.bicep
    alerts.bicep
    roleAssignments.bicep
```

## 5.2 File Responsibilities

| File | Purpose |
|---|---|
| `infra/main.bicep` | Composes the environment deployment |
| `infra/env/dev.bicepparam` | Dev-specific SKUs, names, and flags |
| `infra/env/staging.bicepparam` | Staging-specific names and SQL settings |
| `infra/env/prod.bicepparam` | Production SKUs and stricter settings |
| `infra/modules/appServicePlan.bicep` | Creates Linux App Service plan |
| `infra/modules/webApp.bicep` | Creates the main web app and app settings |
| `infra/modules/webAppSlot.bicep` | Creates the `staging` slot and sticky settings |
| `infra/modules/logAnalytics.bicep` | Creates workspace-based monitoring |
| `infra/modules/applicationInsights.bicep` | Creates Application Insights |
| `infra/modules/sqlServer.bicep` | Creates logical SQL server |
| `infra/modules/sqlDatabase.bicep` | Creates each DB with chosen SKU |
| `infra/modules/keyVault.bicep` | Creates Key Vault and access policy / RBAC wiring |
| `infra/modules/alerts.bicep` | Creates baseline alerts for prod |
| `infra/modules/roleAssignments.bicep` | Assigns managed identity roles |

## 5.3 What Bicep Should Parameterize

Parameterize at least:

- `environmentName`
- `location`
- `appServicePlanSku`
- `sqlSku`
- `sqlComputeModel`
- `sqlAutoPauseDelay`
- `webAppName`
- `sqlServerName`
- `databaseName`
- `keyVaultName`
- `enableDiagnostics`
- `enableAlerts`

## 5.4 Resources Not Managed By Bicep

These should be bootstrapped separately because they are not ideal ARM/Bicep targets in this repo:

- GitHub repository environments and secrets
- Microsoft Entra ID app registrations
- GitHub OIDC federated credentials

Create supporting scripts:

```text
scripts/
  bootstrap-github-oidc.ps1
  bootstrap-entra-apps.ps1
```

---

## 6. GitHub CI/CD Strategy

## 6.1 Core Rules

- **Build once**
- **Promote the same artifact**
- **Do not rebuild for staging**
- **Do not rebuild for production**
- **Use GitHub OIDC to log into Azure**
- **Use GitHub environments for env-specific secrets and approvals**

## 6.2 Workflow Files To Create

```text
.github/
  workflows/
    ci.yml
    infra.yml
    deploy-dev.yml
    promote-staging.yml
    promote-prod.yml
```

## 6.3 Workflow Responsibilities

| Workflow | Trigger | Purpose |
|---|---|---|
| `ci.yml` | PRs and pushes | Build, test, package app artifact and DB migration artifact |
| `infra.yml` | Changes under `infra/**` or manual dispatch | Validate and deploy Bicep |
| `deploy-dev.yml` | Push to `develop` after CI | Deploy artifact to dev |
| `promote-staging.yml` | Manual dispatch | Promote tested dev artifact to staging slot |
| `promote-prod.yml` | Manual dispatch after staging sign-off | Promote staged artifact into production via slot swap |

## 6.4 CI Workflow (`ci.yml`)

Run on:

- pull requests to `develop`
- pushes to `develop`
- pushes to `main`

Suggested steps:

1. checkout
2. setup .NET 9
3. setup Node LTS
4. restore backend and frontend dependencies
5. run backend build and tests
6. run frontend build and tests
7. copy Angular dist into backend `wwwroot`
8. `dotnet publish`
9. create EF migration bundle
10. write `release-manifest.json`
11. upload artifacts

Artifact naming:

- `release-${{ github.sha }}`

## 6.5 Dev Deployment Workflow (`deploy-dev.yml`)

Trigger:

- successful push to `develop`

Behavior:

1. download `release-${sha}`
2. login to Azure with OIDC
3. deploy `webapp.zip` to `app-fleethub-dev`
4. run migration bundle against `fleethub-dev`
5. run smoke tests against the dev URL

Dev should be **automatic**. No manual approval is needed.

## 6.6 Staging Promotion Workflow (`promote-staging.yml`)

Trigger:

- `workflow_dispatch`

Inputs:

- source commit SHA or release artifact ID

Behavior:

1. operator selects a successful dev artifact
2. workflow logs into Azure with OIDC
3. deploys the exact `webapp.zip` to `app-fleethub-live` slot `staging`
4. runs migration bundle against `fleethub-staging`
5. runs smoke tests against the staging slot URL
6. records deployment metadata in GitHub environment history

This is the first formal promotion gate.

## 6.7 Production Promotion Workflow (`promote-prod.yml`)

Trigger:

- `workflow_dispatch`
- only after staging validation

Behavior:

1. select the already staged artifact
2. run production prechecks
3. run the migration bundle against the prod database
4. run **slot swap with preview** if desired
5. swap `staging` into `production`
6. run production smoke tests
7. tag the deployment, for example `prod-2026.04.02.1`

Production should not redeploy a newly built package. It should only promote what already passed in staging.

---

## 7. Promotion Flow

## 7.1 Recommended Flow

```text
feature/* push
  -> ci.yml
  -> PR to develop

merge to develop
  -> ci.yml
  -> deploy-dev.yml
  -> dev environment updated automatically

manual promote-staging
  -> exact dev artifact deployed to staging slot
  -> staging validation

manual promote-prod
  -> exact staged artifact swapped into production
  -> production validation
```

## 7.2 Why This Flow Is Preferred

- dev gets fast feedback
- staging is a real gate, not another rebuild
- production receives exactly what was tested in staging
- rollback is faster because App Service slot swap can be reversed

---

## 8. GitHub Environments and Security

Create these GitHub environments:

- `dev`
- `staging`
- `prod`

Store environment-specific values there:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_RESOURCE_GROUP`
- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_SLOT`
- `AZURE_KEYVAULT_NAME`

### Approval Rules

Preferred:

- `dev`: no approval
- `staging`: 1 approver from engineering lead or QA
- `prod`: 2 approvers, including product or operations owner

If the GitHub plan for the repository does not support required reviewers on private repositories, use:

- protected branches
- CODEOWNERS reviews
- manual `workflow_dispatch` for staging and prod promotion

---

## 9. Database Migration Strategy

Use **EF migration bundles** as part of release artifacts.

## 9.1 Rules

- every schema change must be committed as a migration
- staging migration runs during `promote-staging`
- production migration runs during `promote-prod`
- production migrations must be **backward compatible**

Examples of safe production-first changes:

- add nullable columns
- add new tables
- add indexes

Examples that need a controlled multi-step rollout:

- dropping columns
- renaming columns without compatibility support
- tightening nullability on populated columns

## 9.2 Rollback Position

App rollback is fast via slot swap.

Database rollback is not.

Therefore:

- prefer expand/contract migrations
- take an Azure SQL backup/restore point before prod migration
- treat destructive schema changes as separate release activities

---

## 10. Cost Controls

## 10.1 Dev Cost Controls

- use **App Service Free F1** first
- use default `azurewebsites.net` hostname
- use **serverless SQL with auto-pause**
- use a small App Insights daily cap
- use one shared non-prod Key Vault

## 10.2 Staging Cost Controls

- use the **staging slot** instead of a separate paid app
- keep staging DB serverless if usage is intermittent
- share monitoring resources where acceptable

## 10.3 Production Cost Controls

- start on **S1**, not Premium
- do not add Front Door, APIM, or Azure SignalR until a real need exists
- review scaling after actual usage data is available

---

## 11. Recommended Implementation Order

1. Add SPA static-file hosting to the ASP.NET Core app
2. Add Angular-to-`wwwroot` packaging in CI
3. Create `infra/` Bicep structure
4. Bootstrap Entra apps and GitHub OIDC credentials
5. Deploy `dev`
6. Create live app + `staging` slot
7. Add staging and prod promotion workflows
8. Add EF migrations and migration bundle execution
9. Add production alerts and health probes

---

## 12. Final Recommendation

For this repository, the best first production-grade Azure strategy is:

- **one combined App Service app for the SPA + API**
- **one cheap standalone dev environment**
- **one paid live environment with a staging slot**
- **serverless SQL for non-prod**
- **build once and promote**

This is the lowest-cost design that still:

- fits the current Angular + ASP.NET Core + SignalR implementation
- supports a real dev -> staging -> prod promotion flow
- avoids duplicate paid environments where a slot is sufficient
- keeps the future path open for scale-out later

---

## 13. References

- Azure App Service deployment slots: https://learn.microsoft.com/en-us/azure/app-service/deploy-staging-slots
- Azure App Service limits: https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/azure-subscription-service-limits
- Azure App Service pricing: https://azure.microsoft.com/en-us/pricing/details/app-service/linux/
- Azure SQL Database pricing: https://azure.microsoft.com/en-us/pricing/details/azure-sql-database/single/
- GitHub environment management: https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments
- Azure Login with GitHub OIDC: https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure-openid-connect
