# Pruebas de integración local

Ejecutar db/03_claves_seguras.sql sobre una base existente antes de iniciar la API. Usar variables de entorno locales; no guardar credenciales ni tokens en Git.

1. POST /api/login con el administrador; guardar token y cookie de fotos de la respuesta.
2. Confirmar que GET /api/clientes sin token devuelve 401.
3. Crear cliente, equipo y técnico ficticios. Crear dos órdenes y asignarlas al técnico.
4. Editar el equipo y el técnico; clave vacía al editar debe conservar su contraseña. La consulta de técnicos no debe devolver clave.
5. Iniciar sesión como técnico y descargar /api/ordenes/tecnico/{suId}. Consultar otro ID debe devolver 403.
6. Enviar POST /api/inspecciones con idOrden, fechaHoraReal, observaciones, los cinco ítems del checklist y evidencias Base64. Un resultado Malo requiere observación y foto.
7. Repetir el mismo envío: debe devolver el mismo idInspeccion, sin insertar otra fila. Cambiar su contenido debe producir 409.
8. Verificar como administrador el detalle, la foto, la orden Completada y el historial ?idEquipo=.
9. Cerrar sesión con POST /api/logout y confirmar que el token anterior devuelve 401.
10. Ejecutar IntegracionLocalTest de la rama feature/fotos-evidencia para comprobar SQLite, FIFO, JobScheduler, imágenes y solo lectura en Android. Se necesitan dos órdenes Pendiente nuevas para cada ejecución.

Las pruebas unitarias se ejecutan con dotnet test. No equivalen por sí solas a la comprobación del circuito móvil y web ni a una certificación de seguridad de un despliegue.
