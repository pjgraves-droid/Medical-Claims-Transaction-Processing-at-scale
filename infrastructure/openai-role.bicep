@description('Name of the existing Azure OpenAI account')
param openAiName string

@description('Principal IDs to grant the "Cognitive Services OpenAI User" role')
param principalIds array

resource openAi 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: openAiName
}

@description('Built-in "Cognitive Services OpenAI User" role. See https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/role-based-access-control')
resource openAiUserRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
}

resource roleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in principalIds: {
  name: guid(openAi.id, openAiUserRole.id, principalId)
  scope: openAi
  properties: {
    roleDefinitionId: openAiUserRole.id
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}]
