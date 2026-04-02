using '../main.bicep'

param environmentName = 'staging'
param location = 'canadacentral'
param tags = {
  application: 'IronvaleFleetHub'
  environment: 'staging'
  owner: 'platform-team'
}

param logAnalyticsName = 'log-fleethub-staging'
param applicationInsightsName = 'appi-fleethub-staging'
param keyVaultName = 'kv-fleethub-staging'

param appServicePlanName = 'asp-fleethub-staging'
param appServicePlanSkuName = 'B1'
param appServicePlanSkuTier = 'Basic'
param appServicePlanCapacity = 1
param appServicePlanReserved = true

param webAppName = 'app-fleethub-staging'
param webAppRuntime = 'DOTNETCORE|9.0'
param webAppAlwaysOn = true

param azureAdTenantId = '00000000-0000-0000-0000-000000000000'
param azureAdClientId = '00000000-0000-0000-0000-000000000000'

param productionAspNetCoreEnvironment = 'Staging'
param productionUseDevMode = false
param productionAppSettings = {
  'Serilog__MinimumLevel__Default': 'Information'
}

param deployStagingSlot = false
param deployStagingDatabase = false

param sqlServerName = 'sql-fleethub-staging'
param sqlAdministratorLogin = 'sqladminuser'
param sqlAdministratorPassword = 'REPLACE_WITH_SECURE_PASSWORD'
param sqlDatabaseName = 'sqldb-fleethub-staging'
param sqlSkuName = 'GP_S_Gen5_1'
param sqlSkuTier = 'GeneralPurpose'
param sqlSkuCapacity = 1
param sqlComputeModel = 'Serverless'
param sqlMinCapacity = 1
param sqlAutoPauseDelay = 60
param sqlMaxSizeBytes = 34359738368

param createAlerts = false
param webAppRoleDefinitionIds = []
