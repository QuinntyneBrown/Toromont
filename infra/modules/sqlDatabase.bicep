param serverName string
param name string
param location string
param skuName string
param skuTier string = 'GeneralPurpose'
param skuCapacity int = 1
param computeModel string = 'Provisioned'
param minCapacity int = 1
param autoPauseDelay int = -1
param maxSizeBytes int = 34359738368
param tags object = {}

resource provisionedDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = if (computeModel == 'Provisioned') {
  name: '${serverName}/${name}'
  location: location
  sku: {
    name: skuName
    tier: skuTier
    capacity: skuCapacity
  }
  properties: {
    maxSizeBytes: maxSizeBytes
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
  }
  tags: tags
}

resource serverlessDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = if (computeModel == 'Serverless') {
  name: '${serverName}/${name}'
  location: location
  sku: {
    name: skuName
    tier: skuTier
    capacity: skuCapacity
  }
  properties: {
    maxSizeBytes: maxSizeBytes
    minCapacity: minCapacity
    autoPauseDelay: autoPauseDelay
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
  }
  tags: tags
}

output id string = computeModel == 'Serverless' ? serverlessDatabase.id : provisionedDatabase.id
output name string = name
