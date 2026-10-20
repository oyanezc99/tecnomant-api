# TecnoMant API

Servicio web REST de TecnoMant para la app móvil de los técnicos y el portal web: login, órdenes del técnico, sincronización y consulta de inspecciones, resumen de órdenes y mantenimientos de clientes, equipos, técnicos y órdenes. Está hecho con .NET 8 y Dapper sobre SQL Server.

## Requisitos previos

- SDK de .NET 8 o posterior, con el runtime de ASP.NET Core 8.
- SQL Server (cualquier edición, también Express o LocalDB).
- PowerShell, para cargar las variables de entorno.
- Git, para clonar el repositorio.

## Dependencias

Son paquetes NuGet y se descargan solos con `dotnet restore`:

| Proyecto | Paquete | Para qué sirve |
|---|---|---|
| `TecnoMant.Api` | `Dapper` | Consultas a SQL Server. |
| `TecnoMant.Api` | `Microsoft.Data.SqlClient` | Conexión a SQL Server. |
| `TecnoMant.Api` | `Swashbuckle.AspNetCore` | Swagger en desarrollo. |
| `TecnoMant.Api.Tests` | `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | Pruebas unitarias. |

## Instalación

1. Clona el repositorio y entra a la carpeta:

   ```powershell
   git clone https://github.com/oyanezc99/tecnomant-api.git
   cd tecnomant-api
   ```

2. Descarga las dependencias:

   ```powershell
   dotnet restore
   ```

3. Crea la base de datos. Ejecuta en SQL Server, en este orden, `db/01_schema.sql` (crea `TecnoMantDB` y sus tablas) y `db/02_seed.sql` (datos de prueba). Puedes abrirlos en SQL Server Management Studio o usar `sqlcmd`:

   ```powershell
   sqlcmd -S localhost -E -C -i db\01_schema.sql
   sqlcmd -S localhost -E -C -i db\02_seed.sql
   ```

   Ejecuta también db/03_claves_seguras.sql antes de iniciar esta versión, tanto en una base nueva como existente. No borra datos; amplía el campo de claves para PBKDF2. Al arrancar, la API convierte las claves antiguas a hashes con sal.

4. Configura las variables de entorno (sección siguiente).

## Variables de entorno

| Variable | Para qué sirve |
|---|---|
| `ConnectionStrings__TecnoMantDB` | Cadena de conexión a `TecnoMantDB`. Sin ella la API no arranca. |
| `Admin__Usuario` | Usuario del administrador del portal. |
| `Admin__Clave` | Clave del administrador del portal. |

Sus valores no están en el repositorio. Copia el ejemplo y complétalo con tus datos:

```powershell
Copy-Item config\db.example.env config\db.local.env
```

`config/db.local.env` queda fuera de git y lleva una línea `NOMBRE=valor` por variable. Para cargarlas en la consola, desde la raíz del proyecto:

```powershell
. .\scripts\cargar-env.ps1
```

Las variables solo viven en esa consola: hay que cargarlas de nuevo en cada consola nueva.

Si faltan `Admin__Usuario` o `Admin__Clave`, la API arranca, pero nadie puede entrar como administrador. Con un SQL Server local sin certificado de confianza, la cadena de conexión necesita `TrustServerCertificate=True`. Con autenticación de Windows, cambia `User Id=...;Password=...` por `Integrated Security=True`.

## Ejecutar la API

En la misma consola donde cargaste las variables:

```powershell
dotnet run --project TecnoMant.Api
```

- API: `http://localhost:5080`. Desde el emulador de Android, `http://10.0.2.2:5080`; desde un celular en la misma red, `http://<IP de esta máquina>:5080`.
- Swagger, solo en desarrollo: `http://localhost:5080/swagger`.
- Las fotos se guardan en `TecnoMant.Api/uploads/` (fuera de git) y se sirven en `/uploads/...`.

La API escucha en todas las interfaces (`0.0.0.0:5080`). Para entrar desde otro equipo o un celular, el firewall de Windows debe permitir conexiones entrantes al puerto 5080.

## Probar

Pruebas unitarias de las reglas. No necesitan base de datos ni variables de entorno:

```powershell
dotnet test TecnoMant.Api.Tests/TecnoMant.Api.Tests.csproj
```

Endpoints: con la API levantada, abre `tecnomant-api.http` en Visual Studio o en VS Code (extensión REST Client) y envía las llamadas en orden. Hay una por cada uno de los 11 endpoints y varios casos de error; cada título dice el código esperado. Abre el editor desde la consola donde cargaste las variables, porque el login de administrador las lee del entorno.

## Volver a los datos iniciales

Las llamadas de `tecnomant-api.http` cambian datos: la orden 3 pasa a `Completada` y se crean y borran clientes. Para volver al estado inicial:

1. Elimina la base `TecnoMantDB` y vuelve a ejecutar `db/01_schema.sql` y `db/02_seed.sql`.
2. Borra la carpeta `TecnoMant.Api/uploads/inspecciones/`.

## Seguridad y sesión local

POST /api/login devuelve rol, nombre, idTecnico y token. Las demás rutas /api requieren Authorization: Bearer TOKEN. El administrador mantiene los datos; un técnico solo descarga sus propias órdenes y envía inspecciones de sus órdenes asignadas. POST /api/logout revoca el token. Los tokens son aleatorios, caducan a las 12 horas y se conservan solo en memoria: un reinicio requiere autenticarse otra vez.

Las claves de los técnicos usan PBKDF2 con sal individual y no aparecen en respuestas. Ejecutar db/03_claves_seguras.sql en bases existentes; el arranque migra los valores antiguos. La credencial de administrador sigue en variables de entorno.

Las fotos en /uploads requieren una sesión de administrador mediante cookie HttpOnly. En desarrollo local se utiliza HTTP; un despliegue requiere HTTPS y una configuración de sesión adecuada para su infraestructura.

## Órdenes e inspecciones

GET /api/ordenes lista todos los estados y admite idEquipo e idTecnico. POST /api/ordenes asigna equipo, técnico y fecha. PUT y DELETE /api/ordenes/{id} permiten gestionar órdenes sin inspección. Una orden se completa al recibir la inspección; no se marca manualmente como completada.

Una inspección debe contener los cinco ítems del protocolo de su tipo de equipo. Malo exige observación y fotografía. El envío se guarda en una transacción y los reintentos idénticos devuelven la inspección existente; un contenido diferente devuelve 409.

Los ejemplos antiguos del archivo .http requieren añadir el token Bearer y completar el checklist de cinco ítems antes de enviarse. Ver scripts/pruebas-integracion-local.md para la verificación actual.
