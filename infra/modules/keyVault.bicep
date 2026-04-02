param name string
param location string
param enableRbacAuthorization bool = true
param enablePurgeProtection bool = false
param publicNetworkAccess string = 'Enabled'
param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: name
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: enableRbacAuthorization
    enablePurgeProtection: enablePurgeProtection
    publicNetworkAccess: publicNetworkAccess
    accessPolicies: []
  }
  tags: tags
}

output id string = keyVault.id
output name string = keyVault.name
output vaultUri string = keyVault.properties.vaultUri
