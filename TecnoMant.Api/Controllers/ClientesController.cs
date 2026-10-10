using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;

namespace TecnoMant.Api.Controllers;

/// <summary>Mantenimiento de clientes para el portal.</summary>
[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private const string SqlListar =
        "SELECT IdCliente, RazonSocial, Direccion, Contacto FROM Cliente ORDER BY IdCliente";

    private const string SqlObtener =
        "SELECT IdCliente, RazonSocial, Direccion, Contacto FROM Cliente WHERE IdCliente = @id";

    // OUTPUT devuelve la fila tal como quedó guardada, con su id.
    private const string SqlInsertar = """
        INSERT INTO Cliente (RazonSocial, Direccion, Contacto)
        OUTPUT INSERTED.IdCliente, INSERTED.RazonSocial, INSERTED.Direccion, INSERTED.Contacto
        VALUES (@razonSocial, @direccion, @contacto)
        """;

    // Si el cliente no existe no se actualiza nada y no devuelve ninguna fila.
    private const string SqlActualizar = """
        UPDATE Cliente
        SET RazonSocial = @razonSocial, Direccion = @direccion, Contacto = @contacto
        OUTPUT INSERTED.IdCliente, INSERTED.RazonSocial, INSERTED.Direccion, INSERTED.Contacto
        WHERE IdCliente = @id
        """;

    private const string SqlEliminar =
        "DELETE FROM Cliente WHERE IdCliente = @id";

    // Error de SQL Server cuando una clave foránea impide la operación.
    private const int ErrorClaveForanea = 547;

    private readonly SqlConnection _conexion;

    public ClientesController(SqlConnection conexion)
    {
        _conexion = conexion;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cliente>>> Listar()
    {
        var clientes = await _conexion.QueryAsync<Cliente>(SqlListar);
        return Ok(clientes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Cliente>> Obtener(int id)
    {
        var cliente = await _conexion.QuerySingleOrDefaultAsync<Cliente>(SqlObtener, new { id });
        if (cliente is null)
        {
            return NotFound(new { mensaje = "El cliente no existe" });
        }

        return Ok(cliente);
    }

    [HttpPost]
    public async Task<ActionResult<Cliente>> Crear([FromBody] Cliente cliente)
    {
        var error = ClienteReglas.Validar(cliente);
        if (error is not null)
        {
            return BadRequest(new { mensaje = error });
        }

        // El idCliente del cuerpo se ignora: el id lo genera la base de datos.
        var creado = await _conexion.QuerySingleAsync<Cliente>(SqlInsertar, new
        {
            razonSocial = cliente.RazonSocial!.Trim(),
            direccion = cliente.Direccion!.Trim(),
            contacto = cliente.Contacto
        });

        return CreatedAtAction(nameof(Obtener), new { id = creado.IdCliente }, creado);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Cliente>> Editar(int id, [FromBody] Cliente cliente)
    {
        var error = ClienteReglas.Validar(cliente);
        if (error is not null)
        {
            return BadRequest(new { mensaje = error });
        }

        // Manda el id de la ruta; el idCliente del cuerpo se ignora.
        var actualizado = await _conexion.QuerySingleOrDefaultAsync<Cliente>(SqlActualizar, new
        {
            id,
            razonSocial = cliente.RazonSocial!.Trim(),
            direccion = cliente.Direccion!.Trim(),
            contacto = cliente.Contacto
        });

        if (actualizado is null)
        {
            return NotFound(new { mensaje = "El cliente no existe" });
        }

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
                return NotFound(new { mensaje = "El cliente no existe" });
            }

            return NoContent();
        }
        catch (SqlException ex) when (ex.Number == ErrorClaveForanea)
        {
            // El cliente tiene equipos (u otros registros) que lo referencian.
            return Conflict(new { mensaje = "No se puede eliminar: tiene registros relacionados" });
        }
    }
}
