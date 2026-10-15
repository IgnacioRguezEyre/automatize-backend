# automatize-backend

[![Empleos CI](https://github.com/IgnacioRguezEyre/automatize-backend/actions/workflows/empleos-ci.yml/badge.svg?branch=dev)](https://github.com/IgnacioRguezEyre/automatize-backend/actions/workflows/empleos-ci.yml)
[![Integraciones CI](https://github.com/IgnacioRguezEyre/automatize-backend/actions/workflows/integraciones-ci.yml/badge.svg?branch=dev)](https://github.com/IgnacioRguezEyre/automatize-backend/actions/workflows/integraciones-ci.yml)

## Base de datos local

SQL Server 2025 (edición Developer) en Docker. Una base de datos por microservicio; la de Empleos se llama `Jobs` y la crean las migraciones de Entity Framework.

1. Abre Docker Desktop y espera a que ponga *Engine running*.
2. Copia `.env.example` como `.env` y pon una contraseña.
3. En la raíz del repositorio: `docker compose up -d`
4. Comprueba que está lista: `docker compose ps` (estado `healthy`, tarda unos 30 segundos la primera vez).

Para conectar desde SQL Server Management Studio: servidor `localhost,1433`, autenticación de SQL Server, usuario `sa`, la contraseña del `.env` y marca *Certificado de servidor de confianza*.

La contraseña no va en `appsettings`: guárdala en los secretos de usuario del proyecto API.

```
cd services/empleos/src/Empleos.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Jobs" "Server=localhost,1433;Database=Jobs;User Id=sa;Password=<la del .env>;TrustServerCertificate=True"
```

Para parar: `docker compose down` (los datos se conservan en el volumen `sqlserver-data`; `docker compose down -v` los borra).

## Migraciones (servicio Empleos)

En desarrollo la API aplica sola las migraciones pendientes al arrancar (`Database:MigrateOnStartup` en `appsettings.Development.json`). En producción no: las aplicará el pipeline de despliegue.

Herramienta, una sola vez: `dotnet tool install --global dotnet-ef`

Desde `services/empleos`:

```
dotnet ef migrations add <Nombre> --project src/Empleos.Infrastructure --startup-project src/Empleos.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Empleos.Infrastructure --startup-project src/Empleos.Api
```

Las tablas del servicio van en el esquema `jobs`.

## API de Empleos

| Método | Ruta | Qué hace |
|---|---|---|
| GET | `/api/job-offers?status=New&search=net&page=1&pageSize=20` | Lista paginada. Filtros opcionales: estado (`New`, `Viewed`, `Applied`, `Discarded`) y texto en título o empresa |
| GET | `/api/job-offers/{id}` | Una oferta, con sus novedades y notas |
| POST | `/api/job-offers` | Crea una oferta |
| POST | `/api/job-offers/import` | Importa ofertas con su historial. Idempotente por `externalId` |
| PUT | `/api/job-offers/{id}/details` | Cambia ubicación, modalidad, salario, enlace y descripción |
| POST | `/api/job-offers/{id}/view` · `apply` · `unapply` · `discard` · `restore` | Cambia el estado |
| POST | `/api/job-offers/{id}/close` · `reopen` | Marca que ya no acepta candidaturas, o lo quita |
| POST | `/api/job-offers/{id}/events` | Anota una novedad: `CvRead`, `Response`, `Interview` o `Rejection` |
| PUT | `/api/job-offers/{id}/notes` | Guarda las notas |
| GET · POST | `/api/sources` | Lista o crea fuentes (portales y sus remitentes) |
| GET · DELETE | `/api/sources/{id}` | Lee o borra una fuente |
| POST · DELETE | `/api/sources/{id}/senders` · `/senders/{email}` | Añade o quita un remitente |
| POST | `/api/sources/{id}/activate` · `deactivate` | Activa o desactiva la fuente |
| PUT | `/api/sources/{id}/rule` | Cambia la regla de filtrado |

Respuestas de error: 400 si un dato no es válido, 404 si no existe, 409 si se incumple una regla de negocio (aplicar a una descartada, fuente con nombre repetido). Las peticiones de ejemplo están en `src/Empleos.Api/Empleos.Api.http`.

## Servicio Integraciones

Lee las alertas de empleo de Gmail y las envía a Empleos. No tiene base de datos: cada pasada mira los
últimos días (`Sync:LookbackDays`) y Empleos ignora los `externalId` que ya tiene, así que repetir es inocuo.

```
Gmail API ──► GmailMailboxReader ──► MailSyncService ──► analizador del portal ──► EmpleosClient ──► POST /api/job-offers/import
            (Infrastructure)        (Application)      (LinkedIn, Indeed, Glassdoor,   (HttpClient con reintentos)
                                                         InfoJobs, Tecnoempleo)
```

| Método | Ruta | Qué hace |
|---|---|---|
| POST | `/api/sync` | Sincroniza ahora. Con `?dryRun=true` solo devuelve lo que encontraría |
| GET | `/api/sync/query` | La búsqueda de Gmail que se usa |
| GET | `/health` | Salud del servicio |

Además se sincroniza solo cada `Sync:IntervalMinutes` (60 por defecto; desactivado en desarrollo).
Cómo conectar Gmail: [`docs/gmail-setup.md`](docs/gmail-setup.md).

## Tests

| Proyecto | Qué prueba | Necesita |
|---|---|---|
| `Empleos.Domain.Tests` | Reglas de las entidades `JobOffer` y `Source` | Nada |
| `Empleos.Application.Tests` | Casos de uso, con repositorios en memoria y un reloj fijo | Nada |
| `Empleos.Api.IntegrationTests` | La API completa contra un SQL Server temporal en Docker (Testcontainers), aplicando las migraciones | Docker Desktop arrancado |

| `Integraciones.Tests` | Analizadores contra correos reales anonimizados, sincronización, cliente HTTP (reintentos) y API | Nada |

`dotnet test` desde la carpeta de cada servicio lanza todos los suyos.

## Integración y entrega continuas

Cada servicio tiene su workflow (`empleos-ci.yml`, `integraciones-ci.yml`) y solo se ejecuta si cambian sus archivos. El de Empleos se ejecuta en GitHub en cada push a `dev` o `main` y en cada pull request:

1. Compila la solución con .NET 10.
2. Falla si algún paquete (también los transitivos) tiene vulnerabilidades conocidas.
3. Pasa los tests de dominio, aplicación e integración (estos últimos con SQL Server en Docker).
4. Construye la imagen Docker de la API. En `main` además la publica en `ghcr.io/ignaciorguezeyre/automatize-empleos`.

Para probar la imagen en local junto a la base de datos: `docker compose --profile app up -d --build` y la API queda en `http://localhost:5080`.

## Despliegue en Azure

Infraestructura como código en `infra/azure/main.bicep`: Azure Container Apps (escala a cero) y Azure SQL serverless con la oferta gratuita. GitHub Actions entra en Azure con OIDC, sin contraseñas guardadas.

- `.github/workflows/infra.yml`: crea o actualiza la infraestructura (a mano).
- Job `deploy` de `empleos-ci.yml`: en cada merge a `main` aplica las migraciones con un *migration bundle* y despliega la imagen nueva.

En producción la API exige la cabecera `X-Api-Key` en `/api/*` y expone `/health` para las comprobaciones de Azure. La guía paso a paso está en [`docs/azure-setup.md`](docs/azure-setup.md).
