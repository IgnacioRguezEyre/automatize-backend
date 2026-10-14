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
