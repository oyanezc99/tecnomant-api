using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;

namespace TecnoMant.Api.Controllers;

/// <summary>Mantenimiento de equipos para el portal.</summary>
[ApiController]
[Route("api/equipos")]
public class EquiposController : ControllerBase
{
    // Cada equipo trae la razón social de su cliente (campo de solo lectura "cliente").
    private const string SqlListar = """
        SELECT e.IdEquipo, e.IdCliente, e.Tipo, e.CodigoSerie, e.Ubicacion, c.RazonSocial AS Cliente
        FROM Equipo e
        JOIN Cliente c ON c.IdCliente = e.IdCliente
        ORDER BY e.IdEquipo
        """;

    private const string SqlObtener = """
        SELECT e.IdEquipo, e.IdCliente, e.Tipo, e.CodigoSerie, e.Ubicacion, c.RazonSocial AS Cliente
        FROM Equipo e
        JOIN Cliente c ON c.IdCliente = e.IdCliente
        WHERE e.IdEquipo = @id
        """;

    private const string SqlExisteCliente =
        "SELECT COUNT(1) FROM Cliente WHERE IdCliente = @idCliente";

    // OUTPUT devuelve solo el id: la razón social del cliente no está en la tabla Equipo,
    // así que el equipo completo se lee después con SqlObtener.
    private const string SqlInsertar = """
        INSERT INTO Equipo (IdCliente, Tipo, CodigoSerie, Ubicacion)
        OUTPUT INSERTED.IdEquipo
        VALUES (@idCliente, @tipo, @codigoSerie, @ubicacion)
        """;

    // Si el equipo no existe no se actualiza ninguna fila.
    private const string SqlActualizar = """
        UPDATE Equipo
        SET IdCliente = @idCliente, Tipo = @tipo, CodigoSerie = @codigoSerie, Ubicacion = @ubicacion
        WHERE IdEquipo = @id
        """;

    private const string SqlEliminar =
        "DELETE FROM Equipo WHERE IdEquipo = @id";

    // Error de SQL Server cuando una clave foránea impide la operación.
    private const int ErrorClaveForanea = 547;

    private readonly SqlConnection _conexion;

    public EquiposController(SqlConnection conexion)
    {
        _conexion = conexion;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Equipo>>> Listar()
    {
        var equipos = await _conexion.QueryAsync<Equipo>(SqlListar);
        return Ok(equipos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Equipo>> Obtener(int id)
    {
        var equipo = await _conexion.QuerySingleOrDefaultAsync<Equipo>(SqlObtener, new { id });
        if (equipo is null)
        {
            return NotFound(new { mensaje = "El equipo no existe" });
        }

        return Ok(equipo);
    }

    [HttpPost]
    public async Task<ActionResult<Equipo>> Crear([FromBody] Equipo equipo)
    {
        var error = await Validar(equipo);
        if (error is not null)
        {
            return BadRequest(new { mensaje = error });
        }

        // El idEquipo y el cliente del cuerpo se ignoran: el id lo genera la base de datos
        // y la razón social sale del cliente.
        var idEquipo = await _conexion.QuerySingleAsync<int>(SqlInsertar, new
        {
            idCliente = equipo.IdCliente!.Value,
            tipo = equipo.Tipo,
            codigoSerie = equipo.CodigoSerie!.Trim(),
            ubicacion = equipo.Ubicacion
        });

        var creado = await _conexion.QuerySingleAsync<Equipo>(SqlObtener, new { id = idEquipo });
        return CreatedAtAction(nameof(Obtener), new { id = idEquipo }, creado);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Equipo>> Editar(int id, [FromBody] Equipo equipo)
    {
        var error = await Validar(equipo);
        if (error is not null)
        {
            return BadRequest(new { mensaje = error });
        }

        // Manda el id de la ruta; el idEquipo y el cliente del cuerpo se ignoran.
        var filas = await _conexion.ExecuteAsync(SqlActualizar, new
        {
            id,
            idCliente = equipo.IdCliente!.Value,
            tipo = equipo.Tipo,
            codigoSerie = equipo.CodigoSerie!.Trim(),
            ubicacion = equipo.Ubicacion
        });

        if (filas == 0)
        {
            return NotFound(new { mensaje = "El equipo no existe" });
        }

        var actualizado = await _conexion.QuerySingleAsync<Equipo>(SqlObtener, new { id });
        return Ok(actualizado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var filas = await _conexion.ExecuteAsync(SqlEliminar, new { id });
            if (filas == 0)
            {
                return NotFound(new { mensaje = "El equipo no existe" });
            }

            return NoContent();
        }
        catch (SqlException ex) when (ex.Number == ErrorClaveForanea)
        {
            // El equipo tiene órdenes de trabajo que lo referencian.
            return Conflict(new { mensaje = "No se puede eliminar: tiene registros relacionados" });
        }
    }

    // Validaciones comunes de crear y editar: primero el cuerpo, sin tocar la base de datos,
    // y después que el cliente exista. Devuelve el primer mensaje de error, o null.
    private async Task<string?> Validar(Equipo equipo)
    {
        var error = EquipoReglas.Validar(equipo);
        if (error is not null)
        {
            return error;
        }

        var existeCliente = await _conexion.ExecuteScalarAsync<int>(SqlExisteCliente, new { idCliente = equipo.IdCliente });
        if (existeCliente == 0)
        {
            return "idCliente no corresponde a un cliente existente";
        }

        return null;
    }
}
