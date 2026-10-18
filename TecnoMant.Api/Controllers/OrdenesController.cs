using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;

namespace TecnoMant.Api.Controllers;

/// <summary>
/// Órdenes del técnico (app), resumen por estado (dashboard del portal)
/// y mantenimiento de órdenes de trabajo (portal).
/// </summary>
[ApiController]
[Route("api/ordenes")]
public class OrdenesController : ControllerBase
{
    private const string SqlExisteTecnico =
        "SELECT COUNT(1) FROM Tecnico WHERE IdTecnico = @idTecnico";

    private const string SqlExisteEquipo =
        "SELECT COUNT(1) FROM Equipo WHERE IdEquipo = @idEquipo";

    // Cada orden trae, de solo lectura, el cliente y los datos del equipo y el nombre del técnico.
    private const string SqlListar = """
        SELECT o.IdOrden,
               o.IdEquipo,
               o.IdTecnico,
               o.FechaProgramada,
               o.Estado,
               c.RazonSocial AS Cliente,
               e.Tipo        AS EquipoTipo,
               e.CodigoSerie AS EquipoCodigo,
               t.Nombre      AS Tecnico
        FROM OrdenTrabajo o
        JOIN Equipo e  ON e.IdEquipo  = o.IdEquipo
        JOIN Cliente c ON c.IdCliente = e.IdCliente
        JOIN Tecnico t ON t.IdTecnico = o.IdTecnico
        ORDER BY o.IdOrden
        """;

    private const string SqlObtener = """
        SELECT o.IdOrden,
               o.IdEquipo,
               o.IdTecnico,
               o.FechaProgramada,
               o.Estado,
               c.RazonSocial AS Cliente,
               e.Tipo        AS EquipoTipo,
               e.CodigoSerie AS EquipoCodigo,
               t.Nombre      AS Tecnico
        FROM OrdenTrabajo o
        JOIN Equipo e  ON e.IdEquipo  = o.IdEquipo
        JOIN Cliente c ON c.IdCliente = e.IdCliente
        JOIN Tecnico t ON t.IdTecnico = o.IdTecnico
        WHERE o.IdOrden = @id
        """;

    // OUTPUT devuelve solo el id: los campos de solo lectura salen de otras tablas,
    // así que la orden completa se lee después con SqlObtener.
    private const string SqlInsertar = """
        INSERT INTO OrdenTrabajo (IdEquipo, IdTecnico, FechaProgramada, Estado)
        OUTPUT INSERTED.IdOrden
        VALUES (@idEquipo, @idTecnico, @fechaProgramada, @estado)
        """;

    // Con @estado nulo, COALESCE deja el estado que ya tenía. Si la orden no existe
    // no se actualiza ninguna fila.
    private const string SqlActualizar = """
        UPDATE OrdenTrabajo
        SET IdEquipo = @idEquipo, IdTecnico = @idTecnico, FechaProgramada = @fechaProgramada,
            Estado = COALESCE(@estado, Estado)
        WHERE IdOrden = @id
        """;

    private const string SqlEliminar =
        "DELETE FROM OrdenTrabajo WHERE IdOrden = @id";

    // Error de SQL Server cuando una clave foránea impide la operación.
    private const int ErrorClaveForanea = 547;

    private const string SqlOrdenesDelTecnico = """
        SELECT o.IdOrden,
               c.RazonSocial AS Cliente,
               c.Direccion,
               e.Tipo        AS EquipoTipo,
               e.CodigoSerie AS EquipoCodigo,
               e.Ubicacion,
               o.FechaProgramada,
               o.Estado
        FROM OrdenTrabajo o
        JOIN Equipo e  ON e.IdEquipo  = o.IdEquipo
        JOIN Cliente c ON c.IdCliente = e.IdCliente
        WHERE o.IdTecnico = @idTecnico
          AND o.Estado IN ('Pendiente', 'En proceso')
        ORDER BY o.FechaProgramada, o.IdOrden
        """;

    // Sin GROUP BY siempre devuelve una fila; COALESCE da 0 cuando no hay órdenes.
    private const string SqlResumen = """
        SELECT COALESCE(SUM(CASE WHEN Estado = 'Pendiente'  THEN 1 ELSE 0 END), 0) AS Pendientes,
               COALESCE(SUM(CASE WHEN Estado = 'En proceso' THEN 1 ELSE 0 END), 0) AS EnProceso,
               COALESCE(SUM(CASE WHEN Estado = 'Completada' THEN 1 ELSE 0 END), 0) AS Completadas
        FROM OrdenTrabajo
        """;

    private readonly SqlConnection _conexion;

    public OrdenesController(SqlConnection conexion)
    {
        _conexion = conexion;
    }

    /// <summary>Órdenes Pendiente o En proceso del técnico, por fecha programada.</summary>
    [HttpGet("tecnico/{idTecnico:int}")]
    public async Task<ActionResult<IEnumerable<OrdenTecnico>>> ListarDelTecnico(int idTecnico)
    {
        var existe = await _conexion.ExecuteScalarAsync<int>(SqlExisteTecnico, new { idTecnico });
        if (existe == 0)
        {
            return NotFound(new { mensaje = "El técnico no existe" });
        }

        var ordenes = await _conexion.QueryAsync<OrdenTecnico>(SqlOrdenesDelTecnico, new { idTecnico });
        return Ok(ordenes);
    }

    /// <summary>Número de órdenes en cada estado.</summary>
    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenOrdenes>> Resumir()
    {
        var resumen = await _conexion.QuerySingleAsync<ResumenOrdenes>(SqlResumen);
        return Ok(resumen);
    }

    // Mantenimiento de órdenes. Las rutas literales "resumen" y "tecnico/{idTecnico}"
    // tienen prioridad sobre "{id:int}", así que no se confunden con estas.

    /// <summary>Todas las órdenes, por idOrden.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Orden>>> Listar()
    {
        var ordenes = await _conexion.QueryAsync<Orden>(SqlListar);
        return Ok(ordenes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Orden>> Obtener(int id)
    {
        var orden = await _conexion.QuerySingleOrDefaultAsync<Orden>(SqlObtener, new { id });
        if (orden is null)
        {
            return NotFound(new { mensaje = "La orden no existe" });
        }

        return Ok(orden);
    }

    /// <summary>Crea una orden. Sin estado, se crea en Pendiente.</summary>
    [HttpPost]
    public async Task<ActionResult<Orden>> Crear([FromBody] Orden orden)
    {
        var error = await Validar(orden);
        if (error is not null)
        {
            return BadRequest(new { mensaje = error });
        }

        // El idOrden y los campos de solo lectura del cuerpo se ignoran.
        var idOrden = await _conexion.QuerySingleAsync<int>(SqlInsertar, new
        {
            idEquipo = orden.IdEquipo!.Value,
            idTecnico = orden.IdTecnico!.Value,
            fechaProgramada = orden.FechaProgramada!.Value,
            estado = OrdenReglas.EstadoInformado(orden.Estado) ?? OrdenReglas.EstadoPorDefecto
        });

        var creada = await _conexion.QuerySingleAsync<Orden>(SqlObtener, new { id = idOrden });
        return CreatedAtAction(nameof(Obtener), new { id = idOrden }, creada);
    }

    /// <summary>Edita una orden en cualquier estado. Sin estado, conserva el que tenía.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Orden>> Editar(int id, [FromBody] Orden orden)
    {
        var error = await Validar(orden);
        if (error is not null)
        {
            return BadRequest(new { mensaje = error });
        }

        // Manda el id de la ruta; el idOrden y los campos de solo lectura del cuerpo se ignoran.
        var filas = await _conexion.ExecuteAsync(SqlActualizar, new
        {
            id,
            idEquipo = orden.IdEquipo!.Value,
            idTecnico = orden.IdTecnico!.Value,
            fechaProgramada = orden.FechaProgramada!.Value,
            estado = OrdenReglas.EstadoInformado(orden.Estado)
        });

        if (filas == 0)
        {
            return NotFound(new { mensaje = "La orden no existe" });
        }

        var actualizada = await _conexion.QuerySingleAsync<Orden>(SqlObtener, new { id });
        return Ok(actualizada);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var filas = await _conexion.ExecuteAsync(SqlEliminar, new { id });
            if (filas == 0)
            {
                return NotFound(new { mensaje = "La orden no existe" });
            }

            return NoContent();
        }
        catch (SqlException ex) when (ex.Number == ErrorClaveForanea)
        {
            // La orden ya tiene una inspección que la referencia.
            return Conflict(new { mensaje = "No se puede eliminar: tiene registros relacionados" });
        }
    }

    // Validaciones comunes de crear y editar: primero el cuerpo, sin tocar la base de datos,
    // y después que existan el equipo y el técnico. Devuelve el primer mensaje de error, o null.
    private async Task<string?> Validar(Orden orden)
    {
        var error = OrdenReglas.Validar(orden);
        if (error is not null)
        {
            return error;
        }

        var existeEquipo = await _conexion.ExecuteScalarAsync<int>(SqlExisteEquipo, new { idEquipo = orden.IdEquipo });
        if (existeEquipo == 0)
        {
            return "idEquipo no corresponde a un equipo existente";
        }

        var existeTecnico = await _conexion.ExecuteScalarAsync<int>(SqlExisteTecnico, new { idTecnico = orden.IdTecnico });
        if (existeTecnico == 0)
        {
            return "idTecnico no corresponde a un técnico existente";
        }

        return null;
    }
}
