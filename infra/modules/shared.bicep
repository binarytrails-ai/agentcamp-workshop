@description('Azure region for resource deployment')
param location string

@description('Tags to apply to all resources')
param tags object

@description('Name of the Log Analytics Workspace')
param logAnalyticsWorkspaceName string

@description('Name of the Application Insights resource')
param appInsightsName string

@description('Log retention period in days for Log Analytics')
@minValue(7)
@maxValue(730)
param logRetentionInDays int = 7

// Log Analytics Workspace for centralized logging and monitoring
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

// Application Insights for production observability
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

output logAnalyticsWorkspaceId string = logAnalytics.id
output logAnalyticsWorkspaceName string = logAnalytics.name
output appInsightsResourceId string = appInsights.id
output appInsightsName string = appInsights.name
// Connection string is the preferred method for accessing Application Insights
output appInsightsConnectionString string = appInsights.properties.ConnectionString
