using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;

namespace TecnoMant.Api.Controllers;

/// <summary>Login único para el técnico (app) y el administrador (portal).</summary>
[ApiController]
[Route("api/login")]
public class LoginController : ControllerBase
{
    private const string SqlTecnicosPorUsuario =
        "SELECT IdTecnico, Nombre, Clave FROM Tecnico WHERE Usuario = @usuario ORDER BY IdTecnico";

    private readonly SqlConnection _conexion;
    private readonly IConfiguration _configuracion;

    public LoginController(SqlConnection conexion, IConfiguration configuracion)
    {
        _conexion = conexion;
        _configuracion = configuracion;
    }

    [HttpPost]
    public async Task<ActionResult<LoginRespuesta>> Entrar([FromBody] LoginPedido pedido)
    {
        var error = LoginReglas.Validar(pedido);
        if (error is not null)
        {
            return BadRequest(new { mensaje = error });
        }

        var usuario = pedido.Usuario!.Trim();
        var clave = pedido.Clave!;

        // Primero, el administrador de la configuración (Admin__Usuario y Admin__Clave).
        if (LoginReglas.EsAdministrador(usuario, clave, _configuracion["Admin:Usuario"], _configuracion["Admin:Clave"]))
        {
            return Ok(new LoginRespuesta { Rol = "admin", IdTecnico = null, Nombre = "Administrador" });
        }

        // Después, la tabla Tecnico. La clave se compara aquí y no en la consulta,
        // porque SQL Server no distingue mayúsculas al comparar textos.
        var tecnicos = await _conexion.QueryAsync<TecnicoConClave>(SqlTecnicosPorUsuario, new { usuario });
        var tecnico = tecnicos.FirstOrDefault(fila => LoginReglas.ClaveCoincide(clave, fila.Clave));
        if (tecnico is null)
        {
            return Unauthorized(new { mensaje = "Usuario o clave incorrectos" });
        }

        return Ok(new LoginRespuesta { Rol = "tecnico", IdTecnico = tecnico.IdTecnico, Nombre = tecnico.Nombre });
    }

    // Fila de la consulta. Es privada: la clave nunca sale en una respuesta.
    private sealed class TecnicoConClave
    {
        public int IdTecnico { get; set; }
        public string Nombre { get; set; } = "";
        public string Clave { get; set; } = "";
    }
}
