# Infrastructure Scaffold

This folder contains the initial Bicep scaffold for Ironvale Fleet Hub Azure infrastructure.

## Layout

```text
infra/
  main.bicep
  env/
    dev.bicepparam
    staging.bicepparam
    prod.bicepparam
  modules/
    alerts.bicep
    appServicePlan.bicep
    applicationInsights.bicep
    keyVault.bicep
    logAnalytics.bicep
    roleAssignments.bicep
    sqlDatabase.bicep
    sqlServer.bicep
    webApp.bicep
    webAppSlot.bicep
```

## Intent

- `main.bicep` wires the shared platform resources together.
- `env/dev.bicepparam` is the cheapest standalone shared dev starter.
- `env/staging.bicepparam` is a standalone staging starter.
- `env/prod.bicepparam` reflects the recommended live topology with a production app plus a `staging` slot.

The parameter files are scaffolds. Replace all placeholder names, IDs, logins, and passwords before first deployment.

## Notes

- The scaffold assumes the Angular app will eventually be packaged into the ASP.NET Core app's `wwwroot` and deployed as one artifact.
- Role assignments are currently scoped at the resource group level as a conservative starter pattern.
- The `staging` slot configuration is included in the production/live parameter file because the recommended cost-optimized topology shares one live App Service app.

## Example Commands

```powershell
az deployment group create `
  --resource-group rg-fleethub-dev `
  --template-file infra/main.bicep `
  --parameters infra/env/dev.bicepparam
```

```powershell
az deployment group create `
  --resource-group rg-fleethub-live `
  --template-file infra/main.bicep `
  --parameters infra/env/prod.bicepparam
```

## Tooling

Use a current Azure CLI and Bicep CLI when deploying `.bicepparam` files. Older local tooling may still build `main.bicep` but not understand `.bicepparam`.
