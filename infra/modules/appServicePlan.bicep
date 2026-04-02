param name string
param location string
param skuName string
param skuTier string
param capacity int = 1
param reserved bool = true
param tags object = {}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: name
  location: location
  kind: reserved ? 'linux' : 'app'
  sku: {
    name: skuName
    tier: skuTier
    capacity: capacity
  }
  properties: {
    reserved: reserved
  }
  tags: tags
}

output id string = appServicePlan.id
output name string = appServicePlan.name
