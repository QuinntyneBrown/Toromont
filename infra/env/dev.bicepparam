using '../main.bicep'

param environmentName = 'dev'
param location = 'canadacentral'
param tags = {
  application: 'IronvaleFleetHub'
  environment: 'dev'
  owner: 'platform-team'
}

param logAnalyticsName = 'log-fleethub-dev'
param applicationInsightsName = 'appi-fleethub-dev'
param keyVaultName = 'kv-fleethub-dev'

param appServicePlanName = 'asp-fleethub-dev'
param appServicePlanSkuName = 'F1'
param appServicePlanSkuTier = 'Free'
param appServicePlanCapacity = 1
param appServicePlanReserved = true

param webAppName = 'app-fleethub-dev'
param webAppRuntime = 'DOTNETCORE|9.0'
param webAppAlwaysOn = false

param azureAdTenantId = '00000000-0000-0000-0000-000000000000'
param azureAdClientId = '00000000-0000-0000-0000-000000000000'

param productionAspNetCoreEnvironment = 'Development'
param productionUseDevMode = false
param productionAppSettings = {
  'Serilog__MinimumLevel__Default': 'Information'
}

param deployStagingSlot = false
param deployStagingDatabase = false

param sqlServerName = 'sql-fleethub-dev'
param sqlAdministratorLogin = 'sqladminuser'
param sqlAdministratorPassword = 'REPLACE_WITH_SECURE_PASSWORD'
param sqlDatabaseName = 'sqldb-fleethub-dev'
param sqlSkuName = 'GP_S_Gen5_1'
param sqlSkuTier = 'GeneralPurpose'
param sqlSkuCapacity = 1
param sqlComputeModel = 'Serverless'
param sqlMinCapacity = 1
param sqlAutoPauseDelay = 60
param sqlMaxSizeBytes = 34359738368

param createAlerts = false
param webAppRoleDefinitionIds = []
