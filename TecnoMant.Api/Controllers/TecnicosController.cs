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
            if (string.IsNullOrWhiteSpace(datos.Nombre) || string.IsNullOrWhiteSpace(datos.Especialidad))
            {
                return BadRequest(new { mensaje = "Nombre y especialidad son obligatorios" });
            }

            var nuevoId = await _reglas.CrearAsync(datos);
            return StatusCode(StatusCodes.Status201Created, nuevoId);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] DatosTecnico datos)
        {
            if (string.IsNullOrWhiteSpace(datos.Nombre) || string.IsNullOrWhiteSpace(datos.Especialidad))
            {
                return BadRequest(new { mensaje = "Nombre y especialidad son obligatorios" });
            }

            var actualizado = await _reglas.ActualizarAsync(id, datos);
            if (!actualizado) return NotFound(new { mensaje = "Técnico no encontrado" });

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var eliminado = await _reglas.EliminarAsync(id);
            if (!eliminado) return NotFound(new { mensaje = "Técnico no encontrado" });

            return NoContent();
        }
    }
}