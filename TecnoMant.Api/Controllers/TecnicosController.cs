using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;

namespace TecnoMant.Api.Controllers
{
    [ApiController]
    [Route("api/tecnicos")]
    public class TecnicosController : ControllerBase
    {
        private readonly TecnicoReglas _reglas;

        public TecnicosController(SqlConnection db)
        {
            _reglas = new TecnicoReglas(db);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tecnico>>> Listar()
        {
            var tecnicos = await _reglas.ObtenerTodosAsync();
            return Ok(tecnicos);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Tecnico>> Obtener(int id)
        {
            var tecnico = await _reglas.ObtenerPorIdAsync(id);
            if (tecnico is null) return NotFound(new { mensaje = "Técnico no encontrado" });
            return Ok(tecnico);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Crear([FromBody] DatosTecnico datos)
        {
            if (string.IsNullOrWhiteSpace(datos.Nombre) || string.IsNullOrWhiteSpace(datos.Usuario) || datos.Dni.Length != 8 || !datos.Dni.All(char.IsDigit))
            {
                return BadRequest(new { mensaje = "Nombre y especialidad son obligatorios" });
            }

            if (string.IsNullOrWhiteSpace(datos.Clave)) return BadRequest(new { mensaje = "La clave es obligatoria al crear un técnico" });
            if ((await _reglas.ObtenerTodosAsync()).Any(t => t.Usuario == datos.Usuario)) return Conflict(new { mensaje="El usuario ya existe" });
            int nuevoId;
            try { nuevoId = await _reglas.CrearAsync(datos); }
            catch (SqlException e) when(e.Number is 2601 or 2627) { return Conflict(new { mensaje="El usuario ya existe" }); }
            return StatusCode(StatusCodes.Status201Created, nuevoId);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] DatosTecnico datos)
        {
            if (string.IsNullOrWhiteSpace(datos.Nombre) || string.IsNullOrWhiteSpace(datos.Especialidad))
            {
                return BadRequest(new { mensaje = "Nombre y especialidad son obligatorios" });
            }

            if ((await _reglas.ObtenerTodosAsync()).Any(t => t.IdTecnico != id && t.Usuario == datos.Usuario)) return Conflict(new { mensaje="El usuario ya existe" });
            bool actualizado;
            try { actualizado = await _reglas.ActualizarAsync(id, datos); }
            catch (SqlException e) when(e.Number is 2601 or 2627) { return Conflict(new { mensaje="El usuario ya existe" }); }
            if (!actualizado) return NotFound(new { mensaje = "Técnico no encontrado" });

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            bool eliminado;
            try { eliminado = await _reglas.EliminarAsync(id); }
            catch (SqlException e) when(e.Number==547) { return Conflict(new { mensaje="El técnico tiene órdenes asociadas" }); }
            if (!eliminado) return NotFound(new { mensaje = "Técnico no encontrado" });

            return NoContent();
        }
    }
}