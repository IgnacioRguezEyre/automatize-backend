# Conectar Integraciones con Gmail

El servicio lee el correo con la **API de Gmail** y OAuth 2.0, con permiso **solo de lectura**
(`gmail.readonly`). Se hace una vez y tarda unos 10 minutos.

## 1. Proyecto en Google Cloud

1. Entra en <https://console.cloud.google.com/> con la cuenta de Gmail que recibe las alertas.
2. Arriba, selector de proyectos → **Proyecto nuevo** → nombre `automatize` → Crear.
3. Menú → **APIs y servicios → Biblioteca** → busca **Gmail API** → **Habilitar**.

## 2. Pantalla de consentimiento

1. **APIs y servicios → Pantalla de consentimiento de OAuth** (en la consola nueva: *Google Auth Platform → Branding*).
2. Tipo de usuario **Externo**. Nombre de la app `Automatize`, tu correo como contacto.
3. En **Acceso a datos** (o *Permisos*), añade el permiso `https://www.googleapis.com/auth/gmail.readonly`.
4. En **Público** (*Audience*) deja la app en modo **Prueba** y añade tu cuenta de Gmail en **Usuarios de prueba**.

> En modo Prueba Google no revisa la app, pero los refresh tokens **caducan a los 7 días**. Para uso personal
> continuado, pulsa **Publicar app** cuando todo funcione: como solo pides lectura y la usas tú, no necesitas
> verificación (verás un aviso de "app no verificada" al dar permiso; es normal).

## 3. Cliente OAuth

1. **Credenciales** (*Clients*) → **Crear credenciales → ID de cliente de OAuth**.
2. Tipo de aplicación **App de escritorio**, nombre `automatize-integraciones`.
3. **Descargar JSON**. Guárdalo fuera del repositorio (por ejemplo en Descargas). No lo subas nunca a Git.

## 4. Refresh token

Desde `services/integraciones`:

```powershell
dotnet run --project tools/Integraciones.GmailAuth -- "C:\Users\Ignacio\Downloads\client_secret_xxx.json"
```

Se abre el navegador: elige tu cuenta, acepta el permiso de lectura. La consola imprime tres comandos
`dotnet user-secrets set ...`: ejecútalos tal cual. Comprueba que quedaron guardados:

```powershell
dotnet user-secrets list --project src/Integraciones.Api
```

## 5. Probar

1. Arranca Empleos (perfil `http`, puerto 5216) con la base de datos levantada.
2. Arranca Integraciones (perfil `http`, puerto 5226). Se abre Swagger.
3. `POST /api/sync?dryRun=true`: lee y analiza, sin enviar nada. Revisa las ofertas que devuelve.
4. `POST /api/sync`: las envía a Empleos. Repite y verás que la segunda vez todas salen como `skipped`.

## Si algo falla

| Síntoma | Causa |
|---|---|
| 503 "Gmail no está configurado" | Falta alguno de los tres user-secrets |
| `invalid_grant` en el log | El refresh token caducó (app en modo Prueba, 7 días) o se revocó: repite el paso 4 |
| 502 "Empleos no respondió bien" | Empleos no está arrancado o la URL de `Empleos:BaseUrl` no es la suya |
| Un portal devuelve 0 ofertas | Cambió el formato del correo: guarda uno nuevo anonimizado en `tests/.../Fixtures` y ajusta su analizador |
