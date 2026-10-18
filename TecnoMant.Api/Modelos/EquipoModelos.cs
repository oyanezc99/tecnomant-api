namespace TecnoMant.Api.Modelos;

/// <summary>
/// Equipo, tal como entra y sale por /api/equipos.
/// Al crear y editar, el idEquipo y el cliente del cuerpo se ignoran.
/// </summary>
public class Equipo
{
    public int? IdEquipo { get; set; }
    public int? IdCliente { get; set; }

    /// <summary>Ascensor, Aire acondicionado o Activo industrial.</summary>
    public string? Tipo { get; set; }

    public string? CodigoSerie { get; set; }
    public string? Ubicacion { get; set; }

    /// <summary>Razón social del cliente. Solo lectura: la pone la consulta.</summary>
    public string? Cliente { get; set; }
}
