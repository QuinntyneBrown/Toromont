targetScope = 'resourceGroup'

param environmentName string
param location string = resourceGroup().location
param tags object = {}

param logAnalyticsName string
param applicationInsightsName string
param keyVaultName string

param appServicePlanName string
param appServicePlanSkuName string
param appServicePlanSkuTier string
param appServicePlanCapacity int = 1
param appServicePlanReserved bool = true

param webAppName string
param webAppRuntime string = 'DOTNETCORE|9.0'
param webAppAlwaysOn bool = false
param webAppWebSocketsEnabled bool = true
param webAppHttp20Enabled bool = true
param webAppFtpsState string = 'Disabled'
param webAppMinTlsVersion string = '1.2'

param azureAdInstance string = environment().authentication.loginEndpoint
param azureAdTenantId string
param azureAdClientId string

param productionAspNetCoreEnvironment string = 'Production'
param productionUseDevMode bool = false
param productionAppSettings object = {}
param productionConnectionStrings object = {}

param deployStagingSlot bool = false
param stagingSlotName string = 'staging'
param stagingAspNetCoreEnvironment string = 'Staging'
param stagingUseDevMode bool = false
param stagingAzureAdClientId string = azureAdClientId
param stagingAppSettings object = {}
param stagingConnectionStrings object = {}
param slotStickyAppSettingNames array = [
  'ASPNETCORE_ENVIRONMENT'
  'Authentication__UseDevMode'
  'AzureAd__ClientId'
]
param slotStickyConnectionStringNames array = [
  'DefaultConnection'
]

param sqlServerName string
param sqlAdministratorLogin string
@secure()
param sqlAdministratorPassword string
param sqlDatabaseName string
param sqlSkuName string
param sqlSkuTier string = 'GeneralPurpose'
param sqlSkuCapacity int = 1
param sqlComputeModel string = 'Provisioned'
param sqlMinCapacity int = 1
param sqlAutoPauseDelay int = -1
param sqlMaxSizeBytes int = 34359738368

param deployStagingDatabase bool = false
param stagingSqlDatabaseName string = ''
param stagingSqlSkuName string = 'GP_S_Gen5_1'
param stagingSqlSkuTier string = 'GeneralPurpose'
param stagingSqlSkuCapacity int = 1
param stagingSqlComputeModel string = 'Serverless'
param stagingSqlMinCapacity int = 1
param stagingSqlAutoPauseDelay int = 60
param stagingSqlMaxSizeBytes int = 34359738368

param allowAzureServicesToSql bool = true

param createAlerts bool = false
param alertEmailReceivers array = []

param webAppRoleDefinitionIds array = []

module logAnalytics 'modules/logAnalytics.bicep' = {
  name: 'logAnalytics'
  params: {
    name: logAnalyticsName
    location: location
    tags: tags
  }
}

module applicationInsights 'modules/applicationInsights.bicep' = {
  name: 'applicationInsights'
  params: {
    name: applicationInsightsName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
    tags: tags
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    name: keyVaultName
    location: location
    tags: tags
  }
}

module sqlServer 'modules/sqlServer.bicep' = {
  name: 'sqlServer'
  params: {
    name: sqlServerName
    location: location
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    allowAzureServices: allowAzureServicesToSql
    tags: tags
  }
}

module sqlDatabase 'modules/sqlDatabase.bicep' = {
  name: 'sqlDatabase'
  params: {
    serverName: sqlServer.outputs.name
    name: sqlDatabaseName
    location: location
    skuName: sqlSkuName
    skuTier: sqlSkuTier
    skuCapacity: sqlSkuCapacity
    computeModel: sqlComputeModel
    minCapacity: sqlMinCapacity
    autoPauseDelay: sqlAutoPauseDelay
    maxSizeBytes: sqlMaxSizeBytes
    tags: tags
  }
}

module stagingSqlDatabase 'modules/sqlDatabase.bicep' = if (deployStagingDatabase) {
  name: 'stagingSqlDatabase'
  params: {
    serverName: sqlServer.outputs.name
    name: stagingSqlDatabaseName
    location: location
    skuName: stagingSqlSkuName
    skuTier: stagingSqlSkuTier
    skuCapacity: stagingSqlSkuCapacity
    computeModel: stagingSqlComputeModel
    minCapacity: stagingSqlMinCapacity
    autoPauseDelay: stagingSqlAutoPauseDelay
    maxSizeBytes: stagingSqlMaxSizeBytes
    tags: tags
  }
}

var productionSqlConnectionString = 'Server=tcp:${sqlServer.outputs.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.outputs.name};Persist Security Info=False;User ID=${sqlAdministratorLogin};Password=${sqlAdministratorPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var stagingDatabaseName = deployStagingDatabase ? stagingSqlDatabase.outputs.name : sqlDatabase.outputs.name
var stagingSqlConnectionString = 'Server=tcp:${sqlServer.outputs.fullyQualifiedDomainName},1433;Initial Catalog=${stagingDatabaseName};Persist Security Info=False;User ID=${sqlAdministratorLogin};Password=${sqlAdministratorPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

var generatedProductionAppSettings = {
  'ASPNETCORE_ENVIRONMENT': productionAspNetCoreEnvironment
  'Authentication__UseDevMode': string(productionUseDevMode)
  'AzureAd__Instance': azureAdInstance
  'AzureAd__TenantId': azureAdTenantId
  'AzureAd__ClientId': azureAdClientId
  'ApplicationInsights__ConnectionString': applicationInsights.outputs.connectionString
  'WEBSITE_RUN_FROM_PACKAGE': '1'
}

var generatedStagingAppSettings = {
  'ASPNETCORE_ENVIRONMENT': stagingAspNetCoreEnvironment
  'Authentication__UseDevMode': string(stagingUseDevMode)
  'AzureAd__Instance': azureAdInstance
  'AzureAd__TenantId': azureAdTenantId
  'AzureAd__ClientId': stagingAzureAdClientId
  'ApplicationInsights__ConnectionString': applicationInsights.outputs.connectionString
  'WEBSITE_RUN_FROM_PACKAGE': '1'
}

var effectiveProductionAppSettings = union(productionAppSettings, generatedProductionAppSettings)
var effectiveStagingAppSettings = union(stagingAppSettings, generatedStagingAppSettings)

var effectiveProductionConnectionStrings = union(productionConnectionStrings, {
  DefaultConnection: {
    value: productionSqlConnectionString
    type: 'SQLAzure'
  }
})

var effectiveStagingConnectionStrings = union(stagingConnectionStrings, {
  DefaultConnection: {
    value: stagingSqlConnectionString
    type: 'SQLAzure'
  }
})

module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlan'
  params: {
    name: appServicePlanName
    location: location
    skuName: appServicePlanSkuName
    skuTier: appServicePlanSkuTier
    capacity: appServicePlanCapacity
    reserved: appServicePlanReserved
    tags: tags
  }
}

module webApp 'modules/webApp.bicep' = {
  name: 'webApp'
  params: {
    name: webAppName
    location: location
    serverFarmId: appServicePlan.outputs.id
    runtimeStack: webAppRuntime
    alwaysOn: webAppAlwaysOn
    webSocketsEnabled: webAppWebSocketsEnabled
    http20Enabled: webAppHttp20Enabled
    ftpsState: webAppFtpsState
    minTlsVersion: webAppMinTlsVersion
    appSettings: effectiveProductionAppSettings
    connectionStrings: effectiveProductionConnectionStrings
    tags: tags
  }
}

module webAppSlot 'modules/webAppSlot.bicep' = if (deployStagingSlot) {
  name: 'webAppSlot'
  params: {
    siteName: webApp.outputs.name
    slotName: stagingSlotName
    location: location
    serverFarmId: appServicePlan.outputs.id
    runtimeStack: webAppRuntime
    alwaysOn: webAppAlwaysOn
    webSocketsEnabled: webAppWebSocketsEnabled
    http20Enabled: webAppHttp20Enabled
    ftpsState: webAppFtpsState
    minTlsVersion: webAppMinTlsVersion
    appSettings: effectiveStagingAppSettings
    connectionStrings: effectiveStagingConnectionStrings
    stickyAppSettingNames: slotStickyAppSettingNames
    stickyConnectionStringNames: slotStickyConnectionStringNames
    tags: tags
  }
}

module roleAssignments 'modules/roleAssignments.bicep' = if (length(webAppRoleDefinitionIds) > 0) {
  name: 'roleAssignments'
  params: {
    principalId: webApp.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionIds: webAppRoleDefinitionIds
  }
}

module alerts 'modules/alerts.bicep' = if (createAlerts) {
  name: 'alerts'
  params: {
    namePrefix: environmentName
    appServicePlanResourceId: appServicePlan.outputs.id
    sqlDatabaseResourceId: sqlDatabase.outputs.id
    notificationEmailReceivers: alertEmailReceivers
  }
}

output appServicePlanId string = appServicePlan.outputs.id
output webAppId string = webApp.outputs.id
output webAppDefaultHostname string = webApp.outputs.defaultHostname
output webAppPrincipalId string = webApp.outputs.principalId
output stagingSlotId string = deployStagingSlot ? webAppSlot.outputs.id : ''
output productionDatabaseId string = sqlDatabase.outputs.id
output stagingDatabaseId string = deployStagingDatabase ? stagingSqlDatabase.outputs.id : ''
output sqlServerFullyQualifiedDomainName string = sqlServer.outputs.fullyQualifiedDomainName
output applicationInsightsConnectionString string = applicationInsights.outputs.connectionString
output keyVaultUri string = keyVault.outputs.vaultUri
