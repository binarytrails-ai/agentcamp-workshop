@description('Name of the container registry')
param name string

@description('Azure region for resource deployment')
param location string = resourceGroup().location

@description('Tags to apply to all resources')
param tags object = {}

@description('Indicates whether admin user is enabled')
param adminUserEnabled bool = false

@description('Public network access setting')
param publicNetworkAccess string = 'Enabled'

@description('SKU settings')
param sku object = {
  name: 'Basic' // Basic tier suitable for dev/test scenarios
}

// Azure Container Registry for storing and managing container images
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  tags: tags
  sku: sku
  properties: {
    adminUserEnabled: adminUserEnabled // Prefer managed identities over admin user
    publicNetworkAccess: publicNetworkAccess
  }
}

output loginServer string = containerRegistry.properties.loginServer
output name string = containerRegistry.name
output id string = containerRegistry.id
