namespace TecnoMant.Api.Modelos;

// El pedido y la respuesta son clases distintas a propósito: la respuesta no tiene
// la propiedad Clave, así que la clave no puede salir nunca en una respuesta.

/// <summary>
/// Pedido de POST y PUT /api/tecnicos. Al crear y editar, el idTecnico del cuerpo se ignora.
/// </summary>
public class TecnicoPedido
{
    public int? IdTecnico { get; set; }
    public string? Nombre { get; set; }

    /// <summary>Exactamente 8 dígitos.</summary>
    public string? Dni { get; set; }

    public string? Especialidad { get; set; }
    public string? Usuario { get; set; }

    /// <summary>Obligatoria al crear. En PUT, vacía o nula significa "no cambiar".</summary>
    public string? Clave { get; set; }
}

/// <summary>Técnico, tal como sale por /api/tecnicos. Nunca incluye la clave.</summary>
public class Tecnico
{
    public int IdTecnico { get; set; }
    public string Nombre { get; set; } = "";
    public string Dni { get; set; } = "";
    public string? Especialidad { get; set; }
    public string Usuario { get; set; } = "";
}
