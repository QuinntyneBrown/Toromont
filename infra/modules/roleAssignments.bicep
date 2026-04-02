param principalId string
param principalType string = 'ServicePrincipal'
param roleDefinitionIds array = []

resource roleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for roleDefinitionId in roleDefinitionIds: {
  name: guid(resourceGroup().id, principalId, roleDefinitionId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
    principalId: principalId
    principalType: principalType
  }
}]

output assignmentCount int = length(roleDefinitionIds)
