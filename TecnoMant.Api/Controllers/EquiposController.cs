using Microsoft.AspNetCore.Mvc;
using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;

namespace TecnoMant.Api.Controllers
{
    [ApiController]
    [Route("api/equipos")]
    public class EquiposController : ControllerBase
    {
        private readonly EquipoReglas _reglas;

        public EquiposController(IConfiguration configuration)
        {
            _reglas = new EquipoReglas(configuration);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Equipo>>> Listar()
        {
            var equipos = await _reglas.ObtenerTodosAsync();
            return Ok(equipos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Equipo>> Obtener(int id)
        {
            var equipo = await _reglas.ObtenerPorIdAsync(id);
            if (equipo == null) return NotFound();
            return Ok(equipo);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Crear([FromBody] DatosEquipo datos)
        {
            if (string.IsNullOrWhiteSpace(datos.Tipo) || string.IsNullOrWhiteSpace(datos.CodigoSerie))
            {
                return BadRequest("El tipo y el código de serie son obligatorios.");
            }

            var id = await _reglas.CrearAsync(datos);
            return CreatedAtAction(nameof(Obtener), new { id }, id);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] DatosEquipo datos)
        {
            var actualizado = await _reglas.ActualizarAsync(id, datos);
            if (!actualizado) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var eliminado = await _reglas.EliminarAsync(id);
            if (!eliminado) return NotFound();
            return NoContent();
        }
    }
}