using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;

namespace TecnoMant.Api.Controllers;

/// <summary>
/// Mantenimiento de técnicos para el portal. La clave entra al crear y editar,
/// pero ninguna consulta de este controlador la lee: nunca sale en una respuesta.
/// </summary>
[ApiController]
[Route("api/tecnicos")]
public class TecnicosController : ControllerBase
{
    private const string SqlListar =
        "SELECT IdTecnico, Nombre, DNI AS Dni, Especialidad, Usuario FROM Tecnico ORDER BY IdTecnico";

    private const string SqlObtener =
        "SELECT IdTecnico, Nombre, DNI AS Dni, Especialidad, Usuario FROM Tecnico WHERE IdTecnico = @id";

    // La misma comparación que hace el login (WHERE Usuario = @usuario): SQL Server no distingue
    // mayúsculas, así que "TEC01" cuenta como repetido de "tec01". Con @id = 0 (alta) cuentan
    // todos los técnicos; en la edición, el propio técnico no cuenta como repetido.
    private const string SqlUsuarioRepetido =
        "SELECT COUNT(1) FROM Tecnico WHERE Usuario = @usuario AND IdTecnico <> @id";

    // OUTPUT devuelve la fila tal como quedó guardada, con su id y sin la clave.
    private const string SqlInsertar = """
        INSERT INTO Tecnico (Nombre, DNI, Especialidad, Usuario, Clave)
        OUTPUT INSERTED.IdTecnico, INSERTED.Nombre, INSERTED.DNI AS Dni, INSERTED.Especialidad, INSERTED.Usuario
        VALUES (@nombre, @dni, @especialidad, @usuario, @clave)
        """;

    // Con @clave nula, COALESCE deja la clave que ya tenía. Si el técnico no existe
    // no se actualiza nada y no devuelve ninguna fila.
    private const string SqlActualizar = """
        UPDATE Tecnico
        SET Nombre = @nombre, DNI = @dni, Especialidad = @especialidad, Usuario = @usuario,
            Clave = COALESCE(@clave, Clave)
        OUTPUT INSERTED.IdTecnico, INSERTED.Nombre, INSERTED.DNI AS Dni, INSERTED.Especialidad, INSERTED.Usuario
        WHERE IdTecnico = @id
        """;

    private const string SqlEliminar =
        "DELETE FROM Tecnico WHERE IdTecnico = @id";

    // Error de SQL Server cuando una clave foránea impide la operación.
    private const int ErrorClaveForanea = 547;

    // Id que ningún técnico tiene (IDENTITY empieza en 1): en el alta, todo usuario igual es repetido.
    private const int NingunTecnico = 0;

    private readonly SqlConnection _conexion;

    public TecnicosController(SqlConnection conexion)
    {
        _conexion = conexion;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tecnico>>> Listar()
    {
        var tecnicos = await _conexion.QueryAsync<Tecnico>(SqlListar);
        return Ok(tecnicos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Tecnico>> Obtener(int id)
    {
        var tecnico = await _conexion.QuerySingleOrDefaultAsync<Tecnico>(SqlObtener, new { id });
        if (tecnico is null)
        {
            return NotFound(new { mensaje = "El técnico no existe" });
        }

        return Ok(tecnico);
    }

    [HttpPost]
    public async Task<ActionResult<Tecnico>> Crear([FromBody] TecnicoPedido pedido)
    {
        // Primero se valida el cuerpo (400); después, que el usuario no esté repetido (409).
        var error = TecnicoReglas.ValidarAlta(pedido);
        if (error is not null)
        {
            return BadRequest(new { mensaje = error });
        }

        var usuario = pedido.Usuario!.Trim();
        if (await UsuarioRepetido(usuario, NingunTecnico))
        {
            return Conflict(new { mensaje = "El usuario ya existe" });
        }

        // El idTecnico del cuerpo se ignora: el id lo genera la base de datos.
        // La clave se guarda tal cual, sin recortar, porque el login la compara de forma exacta.
        var creado = await _conexion.QuerySingleAsync<Tecnico>(SqlInsertar, new
        {
            nombre = pedido.Nombre!.Trim(),
            dni = pedido.Dni!.Trim(),
            especialidad = pedido.Especialidad,
            usuario,
            clave = pedido.Clave
        });

        return CreatedAtAction(nameof(Obtener), new { id = creado.IdTecnico }, creado);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Tecnico>> Editar(int id, [FromBody] TecnicoPedido pedido)
    {
        // Primero el cuerpo (400), después el usuario repetido (409) y al final el técnico que se edita (404).
        var error = TecnicoReglas.ValidarEdicion(pedido);
        if (error is not null)
        {
            return BadRequest(new { mensaje = error });
        }

        var usuario = pedido.Usuario!.Trim();
        if (await UsuarioRepetido(usuario, id))
        {
            return Conflict(new { mensaje = "El usuario ya existe" });
        }

        // Manda el id de la ruta; el idTecnico del cuerpo se ignora.
        // Sin clave (nula, vacía o solo espacios) se pasa null y la clave guardada no cambia.
        var actualizado = await _conexion.QuerySingleOrDefaultAsync<Tecnico>(SqlActualizar, new
        {
            id,
            nombre = pedido.Nombre!.Trim(),
            dni = pedido.Dni!.Trim(),
            especialidad = pedido.Especialidad,
            usuario,
            clave = TecnicoReglas.ClaveSinCambio(pedido.Clave) ? null : pedido.Clave
        });

        if (actualizado is null)
        {
            return NotFound(new { mensaje = "El técnico no existe" });
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
                return NotFound(new { mensaje = "El técnico no existe" });
            }

            return NoContent();
        }
        catch (SqlException ex) when (ex.Number == ErrorClaveForanea)
        {
            // El técnico tiene órdenes de trabajo que lo referencian.
            return Conflict(new { mensaje = "No se puede eliminar: tiene registros relacionados" });
        }
    }

    // true si otro técnico (distinto de idPropio) ya usa ese usuario.
    private async Task<bool> UsuarioRepetido(string usuario, int idPropio)
    {
        var repetidos = await _conexion.ExecuteScalarAsync<int>(SqlUsuarioRepetido, new { usuario, id = idPropio });
        return repetidos > 0;
    }
}
