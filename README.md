# automatize-backend
## Base de datos local

PostgreSQL en Docker. Una base de datos por microservicio; la de Empleos se llama `jobs`.

1. Copia `.env.example` como `.env` y pon una contraseña.
2. Arranca: `docker compose up -d`
3. Comprueba que está lista: `docker compose ps` (estado `healthy`).

La contraseña no va en `appsettings`: guárdala en los secretos de usuario del proyecto API.

```
cd services/empleos/src/Empleos.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Jobs" "Host=localhost;Port=5432;Database=jobs;Username=automatize;Password=<la del .env>"
```

Para parar: `docker compose down` (los datos se conservan en el volumen `postgres-data`; `docker compose down -v` los borra).
