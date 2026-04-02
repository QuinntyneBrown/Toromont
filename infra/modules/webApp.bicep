param name string
param location string
param serverFarmId string
param runtimeStack string = 'DOTNETCORE|9.0'
param alwaysOn bool = false
param webSocketsEnabled bool = true
param http20Enabled bool = true
param ftpsState string = 'Disabled'
param minTlsVersion string = '1.2'
param appSettings object = {}
param connectionStrings object = {}
param tags object = {}

resource site 'Microsoft.Web/sites@2022-09-01' = {
  name: name
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

resource siteAppSettings 'Microsoft.Web/sites/config@2022-09-01' = {
  name: '${site.name}/appsettings'
  properties: appSettings
}

resource siteConnectionStrings 'Microsoft.Web/sites/config@2022-09-01' = {
  name: '${site.name}/connectionstrings'
  properties: connectionStrings
}

output id string = site.id
output name string = site.name
output defaultHostname string = site.properties.defaultHostName
output principalId string = site.identity.principalId
