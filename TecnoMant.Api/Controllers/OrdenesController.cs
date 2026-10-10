using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TecnoMant.Api.Modelos;

namespace TecnoMant.Api.Controllers;

/// <summary>Órdenes del técnico (app) y resumen por estado (dashboard del portal).</summary>
[ApiController]
[Route("api/ordenes")]
public class OrdenesController : ControllerBase
{
    private const string SqlExisteTecnico =
        "SELECT COUNT(1) FROM Tecnico WHERE IdTecnico = @idTecnico";

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
}
