using TecnoMant.Api.Modelos;

namespace TecnoMant.Api.Reglas;

/// <summary>Validaciones de un cliente que no necesitan la base de datos.</summary>
public static class ClienteReglas
{
    // Tamaños de las columnas de la tabla Cliente (01_schema.sql).
    public const int MaximoRazonSocial = 150;
    public const int MaximoDireccion = 250;
    public const int MaximoContacto = 150;

    /// <summary>
    /// Devuelve el primer mensaje de error, o null si el cliente es válido.
    /// razonSocial y direccion se miden sin espacios sobrantes, que es como se guardan.
    /// </summary>
    public static string? Validar(Cliente cliente)
    {
        if (string.IsNullOrWhiteSpace(cliente.RazonSocial))
        {
            return "razonSocial es obligatoria";
        }

        if (string.IsNullOrWhiteSpace(cliente.Direccion))
        {
            return "direccion es obligatoria";
        }

        if (cliente.RazonSocial.Trim().Length > MaximoRazonSocial)
        {
            return $"razonSocial admite como máximo {MaximoRazonSocial} caracteres";
        }

        if (cliente.Direccion.Trim().Length > MaximoDireccion)
        {
            return $"direccion admite como máximo {MaximoDireccion} caracteres";
        }

        if (cliente.Contacto is not null && cliente.Contacto.Length > MaximoContacto)
        {
            return $"contacto admite como máximo {MaximoContacto} caracteres";
        }

        return null;
    }
}
