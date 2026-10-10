namespace TecnoMant.Api.Modelos;

/// <summary>Pedido de POST /api/login.</summary>
public class LoginPedido
{
    public string? Usuario { get; set; }
    public string? Clave { get; set; }
}

/// <summary>Respuesta de POST /api/login. Nunca incluye la clave.</summary>
public class LoginRespuesta
{
    /// <summary>"admin" o "tecnico".</summary>
    public string Rol { get; set; } = "";

    /// <summary>Id del técnico; nulo para el administrador.</summary>
    public int? IdTecnico { get; set; }

    public string Nombre { get; set; } = "";
}
