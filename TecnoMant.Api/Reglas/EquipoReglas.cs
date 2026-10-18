using TecnoMant.Api.Modelos;

namespace TecnoMant.Api.Reglas;

/// <summary>Validaciones de un equipo que no necesitan la base de datos.</summary>
public static class EquipoReglas
{
    // Tamaños de las columnas de la tabla Equipo (01_schema.sql).
    public const int MaximoCodigoSerie = 60;
    public const int MaximoUbicacion = 200;

    /// <summary>Tipos de equipo permitidos (db/01_schema.sql).</summary>
    public static readonly IReadOnlyList<string> Tipos = new[] { "Ascensor", "Aire acondicionado", "Activo industrial" };

    /// <summary>
    /// Devuelve el primer mensaje de error, o null si el equipo es válido.
    /// Que el idCliente exista lo comprueba el controlador, porque necesita la base de datos.
    /// codigoSerie se mide sin espacios sobrantes, que es como se guarda.
    /// </summary>
    public static string? Validar(Equipo equipo)
    {
        if (equipo.IdCliente is null)
        {
            return "idCliente es obligatorio";
        }

        if (string.IsNullOrWhiteSpace(equipo.Tipo))
        {
            return "tipo es obligatorio";
        }

        // Comparación exacta, distinguiendo mayúsculas, como el resultado de las inspecciones.
        if (!Tipos.Contains(equipo.Tipo, StringComparer.Ordinal))
        {
            return "tipo debe ser Ascensor, Aire acondicionado o Activo industrial";
        }

        if (string.IsNullOrWhiteSpace(equipo.CodigoSerie))
        {
            return "codigoSerie es obligatorio";
        }

        if (equipo.CodigoSerie.Trim().Length > MaximoCodigoSerie)
        {
            return $"codigoSerie admite como máximo {MaximoCodigoSerie} caracteres";
        }

        // ubicacion es opcional y se guarda tal como llega, igual que el contacto de un cliente.
        if (equipo.Ubicacion is not null && equipo.Ubicacion.Length > MaximoUbicacion)
        {
            return $"ubicacion admite como máximo {MaximoUbicacion} caracteres";
        }

        return null;
    }
}
