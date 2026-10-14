# automatize-backend
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
| GET | `/api/job-offers?status=New` | Lista ofertas, opcionalmente por estado |
| GET | `/api/job-offers/{id}` | Una oferta |
| POST | `/api/job-offers` | Crea una oferta |
| POST | `/api/job-offers/{id}/view` · `apply` · `unapply` · `discard` · `restore` | Cambia el estado |

Una regla de dominio incumplida devuelve 409 (por ejemplo, aplicar a una descartada) y un dato no válido, 400. Las peticiones de ejemplo están en `src/Empleos.Api/Empleos.Api.http`.
