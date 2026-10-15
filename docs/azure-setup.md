# Preparar Azure para el despliegue

Se hace una sola vez. Después, cada merge a `main` despliega solo.

## 1. Cuenta de Azure

Crea la cuenta en https://azure.microsoft.com/free (pide tarjeta para verificar la identidad). Recomendado: en el portal, **Cost Management → Budgets**, crea un presupuesto de 5 € con aviso por correo.

## 2. Grupo de recursos y permisos para GitHub (Azure Cloud Shell)

Abre **Cloud Shell** desde el icono `>_` de la barra superior del portal, elige **Bash** y pega los bloques de uno en uno.

```bash
# Datos de partida
RG=rg-automatize
LOCATION=westeurope
REPO=IgnacioRguezEyre/automatize-backend
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

# Proveedores que usa la infraestructura (la primera vez tardan un par de minutos)
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights
az provider register --namespace Microsoft.Sql

# Grupo de recursos: la "carpeta" donde vivirá todo
az group create -n $RG -l $LOCATION
```

```bash
# Identidad para GitHub Actions: una aplicación de Entra ID con permiso solo sobre ese grupo de recursos
APP_ID=$(az ad app create --display-name automatize-github --query appId -o tsv)
az ad sp create --id $APP_ID
az role assignment create --assignee $APP_ID --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RG

# Federación OIDC: Azure confía en los tokens que GitHub emite para el entorno "production" de este repositorio.
# Así GitHub no necesita guardar ninguna contraseña de Azure.
az ad app federated-credential create --id $APP_ID --parameters "{
  \"name\": \"github-production\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$REPO:environment:production\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}"

# Los tres valores que hay que copiar a GitHub
echo "AZURE_CLIENT_ID=$APP_ID"
echo "AZURE_TENANT_ID=$TENANT_ID"
echo "AZURE_SUBSCRIPTION_ID=$SUBSCRIPTION_ID"
```

## 3. Secrets y variables en GitHub

En el repositorio: **Settings → Secrets and variables → Actions**.

Pestaña **Secrets → New repository secret**:

| Nombre | Valor |
|---|---|
| `AZURE_CLIENT_ID` | El que imprimió Cloud Shell |
| `AZURE_TENANT_ID` | El que imprimió Cloud Shell |
| `AZURE_SUBSCRIPTION_ID` | El que imprimió Cloud Shell |
| `SQL_ADMIN_PASSWORD` | Una contraseña nueva, de 12 o más caracteres con mayúsculas, minúsculas, números y algún símbolo. Sin `'`, `"`, `;` ni `$` |
| `API_KEY` | Una clave larga al azar (por ejemplo, un GUID). Es la que mandarás en la cabecera `X-Api-Key` |

Pestaña **Variables**: de momento ninguna. `AZURE_DEPLOY_ENABLED` se añade en el paso 5.

## 4. Imagen pública en GitHub Container Registry

Azure descarga la imagen de `ghcr.io`, y para no darle credenciales la imagen tiene que ser pública:
tu perfil de GitHub → **Packages → automatize-empleos → Package settings → Change visibility → Public**.

## 5. Crear la infraestructura y activar el despliegue

1. **Actions → Infraestructura en Azure → Run workflow** (rama `main`). Tarda unos minutos. Al final muestra la URL de la API.
2. **Settings → Secrets and variables → Actions → Variables → New repository variable**: `AZURE_DEPLOY_ENABLED` = `true`.
3. **Actions → Empleos CI → Run workflow** sobre `main`: aplica las migraciones y despliega la última imagen.

Desde ese momento, cada merge a `main` compila, prueba, publica la imagen, migra la base de datos y despliega.

## Probar la API desplegada

```
curl https://<url>/health
curl -H "X-Api-Key: <tu API_KEY>" https://<url>/api/job-offers
```

Swagger no está activo en producción. La primera petición tras un rato sin uso tarda unos segundos: el contenedor escala a cero y la base de datos se pausa.
