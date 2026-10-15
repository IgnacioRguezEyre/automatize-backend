// Infraestructura del servicio Empleos en Azure. Se despliega con el workflow "Infraestructura en Azure"
// (o a mano con: az deployment group create -g rg-automatize -f infra/azure/main.bicep -p ...).
// Es idempotente: volver a lanzarlo deja todo igual o aplica solo lo que haya cambiado.

@description('Región de Azure.')
param location string = resourceGroup().location

@description('Prefijo de los nombres de los recursos.')
param appName string = 'automatize'

@description('Imagen de la API (pública en ghcr.io).')
param containerImage string = 'ghcr.io/ignaciorguezeyre/automatize-empleos:latest'

@description('Usuario administrador de SQL.')
param sqlAdminLogin string = 'automatizeadmin'

@secure()
@description('Contraseña del administrador de SQL. Viene de un secret de GitHub; nunca se escribe en el repositorio.')
param sqlAdminPassword string

@secure()
@description('Clave que la API exige en la cabecera X-Api-Key.')
param apiKey string

// uniqueString da siempre el mismo sufijo para este grupo de recursos: el nombre del servidor SQL tiene que ser único en todo Azure.
var suffix = uniqueString(resourceGroup().id)
var sqlServerName = '${appName}-sql-${suffix}'
var databaseName = 'Jobs'

// ---------- Registros (Container Apps los necesita para guardar los logs de los contenedores) ----------
resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${appName}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

// ---------- Base de datos: Azure SQL serverless con la oferta gratuita mensual ----------
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// La regla especial 0.0.0.0 permite el acceso desde servicios de Azure (Container Apps), no desde internet.
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    // Oferta gratuita: un número de horas de cómputo y 32 GB al mes. Al agotarse, la base se pausa
    // hasta el mes siguiente en lugar de cobrar.
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    autoPauseDelay: 60
    minCapacity: json('0.5')
  }
}

// ---------- Entorno de Container Apps ----------
resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${appName}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

// ---------- La API ----------
resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'empleos-api'
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      // Los secretos se guardan cifrados en Azure y el contenedor los recibe como variables de entorno.
      secrets: [
        {
          name: 'jobs-connection'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};User Id=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60'
        }
        {
          name: 'api-key'
          value: apiKey
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'empleos-api'
          image: containerImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'ConnectionStrings__Jobs', secretRef: 'jobs-connection' }
            { name: 'Security__ApiKey', secretRef: 'api-key' }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/health', port: 8080 }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
          ]
        }
      ]
      // Escala a cero cuando nadie la usa: no consume nada. La primera petición tras un rato tarda unos segundos.
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

output apiUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
