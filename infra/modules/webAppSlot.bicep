param siteName string
param slotName string = 'staging'
param location string
param serverFarmId string
param runtimeStack string = 'DOTNETCORE|9.0'
param alwaysOn bool = true
param webSocketsEnabled bool = true
param http20Enabled bool = true
param ftpsState string = 'Disabled'
param minTlsVersion string = '1.2'
param appSettings object = {}
param connectionStrings object = {}
param stickyAppSettingNames array = []
param stickyConnectionStringNames array = []
param tags object = {}

resource slot 'Microsoft.Web/sites/slots@2022-09-01' = {
  name: '${siteName}/${slotName}'
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: serverFarmId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: runtimeStack
      alwaysOn: alwaysOn
      webSocketsEnabled: webSocketsEnabled
      http20Enabled: http20Enabled
      ftpsState: ftpsState
      minTlsVersion: minTlsVersion
    }
  }
  tags: tags
}

resource slotAppSettings 'Microsoft.Web/sites/slots/config@2022-09-01' = {
  name: '${siteName}/${slotName}/appsettings'
  properties: appSettings
}

resource slotConnectionStrings 'Microsoft.Web/sites/slots/config@2022-09-01' = {
  name: '${siteName}/${slotName}/connectionstrings'
  properties: connectionStrings
}

resource slotConfigNames 'Microsoft.Web/sites/config@2022-09-01' = {
  name: '${siteName}/slotConfigNames'
  properties: {
    appSettingNames: stickyAppSettingNames
    connectionStringNames: stickyConnectionStringNames
  }
}

output id string = slot.id
output name string = slot.name
output defaultHostname string = slot.properties.defaultHostName
output principalId string = slot.identity.principalId
