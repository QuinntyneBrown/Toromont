using '../main.bicep'

param environmentName = 'prod'
param location = 'canadacentral'
param tags = {
  application: 'ToromontFleetHub'
  environment: 'prod'
  owner: 'platform-team'
}

param logAnalyticsName = 'log-fleethub-live'
param applicationInsightsName = 'appi-fleethub-live'
param keyVaultName = 'kv-fleethub-live'

param appServicePlanName = 'asp-fleethub-live'
param appServicePlanSkuName = 'S1'
param appServicePlanSkuTier = 'Standard'
param appServicePlanCapacity = 1
param appServicePlanReserved = true

param webAppName = 'app-fleethub-live'
param webAppRuntime = 'DOTNETCORE|9.0'
param webAppAlwaysOn = true

param azureAdTenantId = '00000000-0000-0000-0000-000000000000'
param azureAdClientId = '00000000-0000-0000-0000-000000000000'

param productionAspNetCoreEnvironment = 'Production'
param productionUseDevMode = false
param productionAppSettings = {
  'Serilog__MinimumLevel__Default': 'Information'
}

param deployStagingSlot = true
param stagingSlotName = 'staging'
param stagingAspNetCoreEnvironment = 'Staging'
param stagingUseDevMode = false
param stagingAzureAdClientId = '00000000-0000-0000-0000-000000000000'
param stagingAppSettings = {
  'Serilog__MinimumLevel__Default': 'Information'
}

param sqlServerName = 'sql-fleethub-live'
param sqlAdministratorLogin = 'sqladminuser'
param sqlAdministratorPassword = 'REPLACE_WITH_SECURE_PASSWORD'
param sqlDatabaseName = 'sqldb-fleethub-prod'
param sqlSkuName = 'GP_Gen5_2'
param sqlSkuTier = 'GeneralPurpose'
param sqlSkuCapacity = 2
param sqlComputeModel = 'Provisioned'
param sqlMaxSizeBytes = 34359738368

param deployStagingDatabase = true
param stagingSqlDatabaseName = 'sqldb-fleethub-staging'
param stagingSqlSkuName = 'GP_S_Gen5_1'
param stagingSqlSkuTier = 'GeneralPurpose'
param stagingSqlSkuCapacity = 1
param stagingSqlComputeModel = 'Serverless'
param stagingSqlMinCapacity = 1
param stagingSqlAutoPauseDelay = 60
param stagingSqlMaxSizeBytes = 34359738368

param createAlerts = true
param alertEmailReceivers = [
  {
    name: 'platform-oncall'
    emailAddress: 'platform@example.com'
  }
]

param webAppRoleDefinitionIds = []
