using System.Data.Common;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;

namespace TecnoMant.Api.Controllers;

/// <summary>Sincronización de inspecciones desde la app y su consulta desde el portal.</summary>
[ApiController]
[Route("api/inspecciones")]
public class InspeccionesController : ControllerBase
{
    // UPDLOCK + HOLDLOCK: dos envíos simultáneos de la misma orden pasan de uno en uno,
    // porque el segundo espera aquí hasta que el primero confirma o revierte.
    private const string SqlBloquearOrden =
        "SELECT IdOrden FROM OrdenTrabajo WITH (UPDLOCK, HOLDLOCK) WHERE IdOrden = @idOrden";

    private const string SqlContarInspeccionesDeLaOrden =
        "SELECT COUNT(1) FROM Inspeccion WHERE IdOrden = @idOrden";

    private const string SqlInsertarInspeccion = """
        INSERT INTO Inspeccion (IdOrden, FechaHoraReal, Observaciones, EstadoGeneral)
        OUTPUT INSERTED.IdInspeccion
        VALUES (@idOrden, @fechaHoraReal, @observaciones, @estadoGeneral)
        """;

    private const string SqlInsertarItem = """
        INSERT INTO ItemChecklist (IdInspeccion, NombreItem, Resultado, Observacion)
        VALUES (@idInspeccion, @nombreItem, @resultado, @observacion)
        """;

    // Fecha no se inserta: la pone el valor por defecto de la tabla.
    private const string SqlInsertarEvidencia =
        "INSERT INTO Evidencia (IdInspeccion, RutaFoto) VALUES (@idInspeccion, @rutaFoto)";

    private const string SqlCompletarOrden =
        "UPDATE OrdenTrabajo SET Estado = 'Completada' WHERE IdOrden = @idOrden";

    private const string SqlListado = """
        SELECT i.IdInspeccion,
               i.IdOrden,
               i.FechaHoraReal,
               c.RazonSocial AS Cliente,
               e.Tipo        AS EquipoTipo,
               e.CodigoSerie AS EquipoCodigo,
               t.Nombre      AS Tecnico,
               i.EstadoGeneral
        FROM Inspeccion i
        JOIN OrdenTrabajo o ON o.IdOrden   = i.IdOrden
        JOIN Equipo e       ON e.IdEquipo  = o.IdEquipo
        JOIN Cliente c      ON c.IdCliente = e.IdCliente
        JOIN Tecnico t      ON t.IdTecnico = o.IdTecnico
        WHERE @idEquipo IS NULL OR o.IdEquipo = @idEquipo
        ORDER BY i.FechaHoraReal DESC, i.IdInspeccion DESC
        """;

    private const string SqlDetalle = """
        SELECT i.IdInspeccion,
               i.IdOrden,
               i.FechaHoraReal,
               c.RazonSocial AS Cliente,
               e.Tipo        AS EquipoTipo,
               e.CodigoSerie AS EquipoCodigo,
               t.Nombre      AS Tecnico,
               i.EstadoGeneral,
               i.Observaciones
        FROM Inspeccion i
        JOIN OrdenTrabajo o ON o.IdOrden   = i.IdOrden
        JOIN Equipo e       ON e.IdEquipo  = o.IdEquipo
        JOIN Cliente c      ON c.IdCliente = e.IdCliente
        JOIN Tecnico t      ON t.IdTecnico = o.IdTecnico
        WHERE i.IdInspeccion = @id
        """;

    private const string SqlItems =
        "SELECT NombreItem, Resultado, Observacion FROM ItemChecklist WHERE IdInspeccion = @id ORDER BY IdItem";

    private const string SqlEvidencias =
        "SELECT IdEvidencia, RutaFoto, Fecha FROM Evidencia WHERE IdInspeccion = @id ORDER BY IdEvidencia";

    private readonly SqlConnection _conexion;
    private readonly IWebHostEnvironment _entorno;
    private readonly ILogger<InspeccionesController> _logger;

    public InspeccionesController(
        SqlConnection conexion,
        IWebHostEnvironment entorno,
        ILogger<InspeccionesController> logger)
    {
        _conexion = conexion;
        _entorno = entorno;
        _logger = logger;
    }

    /// <summary>
    /// Recibe una inspección completa (checklist y fotos). Todo ocurre en una transacción:
    /// si algo falla no queda nada a medias, ni en la base de datos ni en la carpeta de fotos.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<InspeccionCreada>> Crear([FromBody] InspeccionPedido pedido)
    {
        // Primero se valida el cuerpo (400); después, que la orden exista (404)
        // y que no tenga ya una inspección (409).
        var error = InspeccionReglas.Validar(pedido, out var fotos);
        if (error is not null)
        {
            return BadRequest(new { mensaje = error });
        }

        var idOrden = pedido.IdOrden!.Value;
        var items = pedido.Items!;
        var estadoGeneral = InspeccionReglas.CalcularEstadoGeneral(items);
        string? carpetaFotos = null;

        await _conexion.OpenAsync();
        await using var tx = await _conexion.BeginTransactionAsync();
        try
        {
            var orden = await _conexion.QuerySingleOrDefaultAsync<int?>(SqlBloquearOrden, new { idOrden }, tx);
            if (orden is null)
            {
                return NotFound(new { mensaje = "La orden no existe" });
            }

            var inspecciones = await _conexion.ExecuteScalarAsync<int>(SqlContarInspeccionesDeLaOrden, new { idOrden }, tx);
            if (inspecciones > 0)
            {
                return Conflict(new { mensaje = "La orden ya tiene una inspección" });
            }

            var idInspeccion = await _conexion.QuerySingleAsync<int>(
                SqlInsertarInspeccion,
                new { idOrden, fechaHoraReal = pedido.FechaHoraReal!.Value, observaciones = pedido.Observaciones, estadoGeneral },
                tx);

            foreach (var item in items)
            {
                await _conexion.ExecuteAsync(
                    SqlInsertarItem,
                    new { idInspeccion, nombreItem = item.NombreItem, resultado = item.Resultado, observacion = item.Observacion },
                    tx);
            }

            if (fotos.Count > 0)
            {
                carpetaFotos = Path.Combine(_entorno.ContentRootPath, "uploads", "inspecciones", idInspeccion.ToString());
                await GuardarFotos(carpetaFotos, idInspeccion, fotos, tx);
            }

            await _conexion.ExecuteAsync(SqlCompletarOrden, new { idOrden }, tx);
            await tx.CommitAsync();

            var creada = new InspeccionCreada
            {
                IdInspeccion = idInspeccion,
                EstadoGeneral = estadoGeneral,
                Evidencias = fotos.Count
            };
            return CreatedAtAction(nameof(Obtener), new { id = idInspeccion }, creada);
        }
        catch
        {
            // La transacción sin confirmar se revierte sola al salir (await using).
            // Aquí se quitan las fotos ya escritas y la excepción sigue hacia el middleware de errores.
            BorrarCarpetaDeFotos(carpetaFotos);
            throw;
        }
    }

    /// <summary>Inspecciones, la más reciente primero. Con ?idEquipo= devuelve solo las de ese equipo.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InspeccionResumen>>> Listar([FromQuery] int? idEquipo)
    {
        var inspecciones = await _conexion.QueryAsync<InspeccionResumen>(SqlListado, new { idEquipo });
        return Ok(inspecciones);
    }

    /// <summary>Detalle de una inspección, con sus ítems y sus fotos.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<InspeccionDetalle>> Obtener(int id)
    {
        var detalle = await _conexion.QuerySingleOrDefaultAsync<InspeccionDetalle>(SqlDetalle, new { id });
        if (detalle is null)
        {
            return NotFound(new { mensaje = "La inspección no existe" });
        }

        detalle.Items = (await _conexion.QueryAsync<ItemDetalle>(SqlItems, new { id })).ToList();

        // La url es la ruta guardada con "/" delante: queda relativa a la URL base.
        var evidencias = await _conexion.QueryAsync<EvidenciaGuardada>(SqlEvidencias, new { id });
        detalle.Evidencias = evidencias
            .Select(fila => new EvidenciaDetalle { IdEvidencia = fila.IdEvidencia, Url = "/" + fila.RutaFoto, Fecha = fila.Fecha })
            .ToList();

        return Ok(detalle);
    }

    // Escribe cada foto como {n}.jpg, con n desde 1, y deja su ruta relativa en Evidencia.
    private async Task GuardarFotos(string carpeta, int idInspeccion, List<byte[]> fotos, DbTransaction tx)
    {
        Directory.CreateDirectory(carpeta);

        for (var n = 1; n <= fotos.Count; n++)
        {
            // System.IO.File con su nombre completo: dentro de un controlador, File es un método.
            await System.IO.File.WriteAllBytesAsync(Path.Combine(carpeta, $"{n}.jpg"), fotos[n - 1]);

            var rutaFoto = $"uploads/inspecciones/{idInspeccion}/{n}.jpg";
            await _conexion.ExecuteAsync(SqlInsertarEvidencia, new { idInspeccion, rutaFoto }, tx);
        }
    }

    private void BorrarCarpetaDeFotos(string? carpeta)
    {
        if (carpeta is null || !Directory.Exists(carpeta))
        {
            return;
        }

        try
        {
            Directory.Delete(carpeta, recursive: true);
        }
        catch (Exception ex)
        {
            // No se relanza: al middleware de errores debe llegar la excepción original.
            _logger.LogError(ex, "No se pudo borrar la carpeta de fotos {Carpeta}", carpeta);
        }
    }

    // Fila de la tabla Evidencia, tal como se guardó.
    private sealed class EvidenciaGuardada
    {
        public int IdEvidencia { get; set; }
        public string RutaFoto { get; set; } = "";
        public DateTime Fecha { get; set; }
    }
}
