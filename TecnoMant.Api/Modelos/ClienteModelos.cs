namespace TecnoMant.Api.Modelos;

/// <summary>
/// Cliente, tal como entra y sale por /api/clientes.
/// Al crear y editar, el idCliente del cuerpo se ignora.
/// </summary>
public class Cliente
{
    public int? IdCliente { get; set; }
    public string? RazonSocial { get; set; }
    public string? Direccion { get; set; }
    public string? Contacto { get; set; }
}
