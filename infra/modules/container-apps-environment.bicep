@description('Name of the Container Apps environment')
param name string

@description('Azure region for resource deployment')
param location string

@description('Tags to apply to all resources')
param tags object = {}

@description('Log Analytics workspace ID for Container Apps diagnostics')
param logAnalyticsWorkspaceId string = ''

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    // Configure App Logs to send to Log Analytics if workspace ID is provided
    appLogsConfiguration: logAnalyticsWorkspaceId != '' ? {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2023-09-01').customerId
        sharedKey: listKeys(logAnalyticsWorkspaceId, '2023-09-01').primarySharedKey
      }
    } : null
  }
}

output name string = containerAppsEnvironment.name
output id string = containerAppsEnvironment.id
output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
