using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.FileProviders;
using TecnoMant.Api;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión llega por configuración (variable de entorno ConnectionStrings__TecnoMantDB).
// Sin ella la API no puede funcionar, así que no arranca.
var cadena = builder.Configuration.GetConnectionString("TecnoMantDB");
if (string.IsNullOrWhiteSpace(cadena))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión: define la variable de entorno ConnectionStrings__TecnoMantDB " +
        "(se carga con scripts/cargar-env.ps1) antes de iniciar la API.");
}

// Una conexión por petición. El contenedor la cierra al terminar la petición.
builder.Services.AddScoped(_ => new SqlConnection(cadena));
builder.Services.AddSingleton<SesionTokens>();

// Vocales acentuadas y eñes sin escapar, para que los mensajes se lean bien.
var codificador = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement);

builder.Services
    .AddControllers()
    .AddJsonOptions(opciones =>
    {
        opciones.JsonSerializerOptions.Converters.Add(new FechaJsonConverter());
        opciones.JsonSerializerOptions.Encoder = codificador;
    })
    .ConfigureApiBehaviorOptions(opciones =>
    {
        // JSON mal formado o tipos de dato inválidos: 400 con {"mensaje": ...}.
        opciones.InvalidModelStateResponseFactory = contexto =>
            new BadRequestObjectResult(new { mensaje = MensajeDeFormato(contexto.ModelState) });

        // Los errores sin cuerpo (404, 415...) no se convierten en ProblemDetails:
        // su cuerpo {"mensaje": ...} lo pone UseStatusCodePages.
        opciones.SuppressMapClientErrors = true;
    });

// Las mismas opciones de JSON para WriteAsJsonAsync, que usan el middleware de errores y UseStatusCodePages.
builder.Services.ConfigureHttpJsonOptions(opciones =>
{
    opciones.SerializerOptions.Converters.Add(new FechaJsonConverter());
    opciones.SerializerOptions.Encoder = codificador;
});

// El portal Angular corre en http://localhost:4200.
builder.Services.AddCors(opciones => opciones.AddDefaultPolicy(politica =>
    politica.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Tamaño del pedido: no se cambia el límite por defecto de Kestrel (30 000 000 bytes, unos 28 MB).
// Cubre el pedido válido más grande: 5 fotos de 2 MB, unos 14 MB en Base64.

var app = builder.Build();
await MigrarClavesAsync(cadena);

app.UseCors();

// Errores inesperados: quedan en el log con la excepción completa y el cliente solo ve "Error interno".
app.Use(async (contexto, siguiente) =>
{
    try
    {
        await siguiente(contexto);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", contexto.Request.Method, contexto.Request.Path);

        if (!contexto.Response.HasStarted)
        {
            contexto.Response.Clear();
            contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await contexto.Response.WriteAsJsonAsync(new { mensaje = "Error interno" });
        }
    }
});

// Respuestas de error que saldrían sin cuerpo, como la de una ruta que no existe.
app.UseStatusCodePages(async contextoDeEstado =>
{
    var respuesta = contextoDeEstado.HttpContext.Response;
    var mensaje = respuesta.StatusCode switch
    {
        StatusCodes.Status404NotFound => "No existe",
        StatusCodes.Status405MethodNotAllowed => "Método no permitido",
        StatusCodes.Status415UnsupportedMediaType => "Tipo de contenido no soportado",
        _ => "Error"
    };

    await respuesta.WriteAsJsonAsync(new { mensaje });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Las fotos se guardan en uploads/ (fuera de git) y se sirven como archivos estáticos en /uploads/...
var carpetaUploads = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(carpetaUploads);
app.Use(async (contexto,siguiente) => {
    if(contexto.Request.Path.StartsWithSegments("/uploads")) {
        var usuario=contexto.RequestServices.GetRequiredService<SesionTokens>().Leer(contexto.Request.Cookies["tecnomant.fotos"]??"");
        if(usuario is null || !usuario.IsInRole("admin")) {contexto.Response.StatusCode=401; return;}
    }
    await siguiente();
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(carpetaUploads),
    RequestPath = "/uploads"
});

// Las rutas administrativas requieren sesión; la app solo consulta sus órdenes y envía inspecciones.
app.Use(async (contexto, siguiente) => {
    var ruta=contexto.Request.Path.Value ?? "";
    if (!ruta.StartsWith("/api/",StringComparison.OrdinalIgnoreCase) || ruta.Equals("/api/login",StringComparison.OrdinalIgnoreCase)) { await siguiente(); return; }
    var cabecera=contexto.Request.Headers.Authorization.ToString();
    var token=cabecera.StartsWith("Bearer ",StringComparison.OrdinalIgnoreCase)?cabecera[7..]:"";
    var usuario=contexto.RequestServices.GetRequiredService<SesionTokens>().Leer(token);
    if(usuario is null) { contexto.Response.StatusCode=401; await contexto.Response.WriteAsJsonAsync(new {mensaje="Inicia sesión nuevamente"}); return; }
    contexto.User=usuario;
    if(ruta=="/api/logout") { contexto.RequestServices.GetRequiredService<SesionTokens>().Cerrar(token); contexto.Response.Cookies.Delete("tecnomant.fotos",new CookieOptions {Path="/uploads"}); contexto.Response.StatusCode=204; return; }
    var id=usuario.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var permitido=usuario.IsInRole("admin") ||
        (contexto.Request.Method=="GET" && ruta=="/api/ordenes/tecnico/"+id) ||
        (contexto.Request.Method=="POST" && ruta=="/api/inspecciones");
    if(!permitido) { contexto.Response.StatusCode=403; await contexto.Response.WriteAsJsonAsync(new {mensaje="No tienes permiso para esta operación"}); return; }
    await siguiente();
});
app.MapControllers();

app.Run();

// Mensaje del 400 cuando el pedido no se puede leer. Nombra el campo cuando la clave del error lo trae.
static string MensajeDeFormato(ModelStateDictionary estado)
{
    var claves = estado
        .Where(par => par.Value is { Errors.Count: > 0 })
        .Select(par => par.Key)
        .ToList();

    // El lector de JSON da la ruta del campo: "$.fechaHoraReal", "$.items[0].resultado".
    var ruta = claves.FirstOrDefault(clave => clave.StartsWith("$.", StringComparison.Ordinal));
    if (ruta is not null)
    {
        return $"El JSON del pedido no es válido: revisa {ruta[2..]}";
    }

    // "$" o clave vacía: falta el cuerpo o no es JSON bien formado.
    if (claves.Any(clave => clave is "" or "$"))
    {
        return "El cuerpo del pedido debe ser un JSON válido";
    }

    // Valores de la ruta o de la consulta, por ejemplo ?idEquipo=abc.
    return claves.Count > 0 ? $"El valor de {claves[0]} no es válido" : "El pedido no es válido";
}

// Conversión local de datos antiguos: no se conservan contraseñas en texto al arrancar.
static async Task MigrarClavesAsync(string cadena)
{
    await using var conexion=new SqlConnection(cadena);
    await conexion.OpenAsync();
    await using var tamano=new SqlCommand("SELECT COL_LENGTH('Tecnico','Clave')",conexion);
    if(Convert.ToInt32(await tamano.ExecuteScalarAsync())<512)
        throw new InvalidOperationException("Ejecuta db/03_claves_seguras.sql antes de arrancar la API");
    var pendientes=new List<(int Id,string Clave)>();
    await using(var consulta=new SqlCommand("SELECT IdTecnico,Clave FROM Tecnico",conexion))
    await using(var lector=await consulta.ExecuteReaderAsync())
        while(await lector.ReadAsync()) if(!lector.GetString(1).StartsWith("pbkdf2$")) pendientes.Add((lector.GetInt32(0),lector.GetString(1)));
    foreach(var tecnico in pendientes)
    {
        await using var actualizar=new SqlCommand("UPDATE Tecnico SET Clave=@hash WHERE IdTecnico=@id AND Clave=@anterior",conexion);
        actualizar.Parameters.AddWithValue("@hash",ClaveSegura.Crear(tecnico.Clave));
        actualizar.Parameters.AddWithValue("@id",tecnico.Id);
        actualizar.Parameters.AddWithValue("@anterior",tecnico.Clave);
        await actualizar.ExecuteNonQueryAsync();
    }
}