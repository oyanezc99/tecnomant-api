using TecnoMant.Api.Modelos;

namespace TecnoMant.Api.Reglas;

/// <summary>Validaciones de una orden de trabajo que no necesitan la base de datos.</summary>
public static class OrdenReglas
{
    /// <summary>Estados permitidos de una orden (RN-04).</summary>
    public static readonly IReadOnlyList<string> Estados = new[] { "Pendiente", "En proceso", "Completada" };

    /// <summary>Estado con el que se crea una orden cuando no llega estado.</summary>
    public const string EstadoPorDefecto = "Pendiente";

    // Rango de la columna DATETIME de SQL Server. Fuera de él la base daría un error 500;
    // aquí se responde 400 con un mensaje.
    public static readonly DateTime FechaMinima = new(1753, 1, 1);
    public static readonly DateTime FechaMaxima = new(9999, 12, 31, 23, 59, 59);

    /// <summary>
    /// Devuelve el primer mensaje de error, o null si la orden es válida.
    /// Que el equipo y el técnico existan lo comprueba el controlador, porque necesita la base de datos.
    /// </summary>
    public static string? Validar(Orden orden)
    {
        if (orden.IdEquipo is null)
        {
            return "idEquipo es obligatorio";
        }

        if (orden.IdTecnico is null)
        {
            return "idTecnico es obligatorio";
        }

        if (orden.FechaProgramada is null)
        {
            return "fechaProgramada es obligatoria";
        }

        if (orden.FechaProgramada < FechaMinima || orden.FechaProgramada > FechaMaxima)
        {
            return "fechaProgramada debe estar entre 1753-01-01 y 9999-12-31";
        }

        // Comparación exacta, distinguiendo mayúsculas, como el resultado de las inspecciones.
        var estado = EstadoInformado(orden.Estado);
        if (estado is not null && !Estados.Contains(estado, StringComparer.Ordinal))
        {
            return "estado debe ser Pendiente, En proceso o Completada";
        }

        return null;
    }

    /// <summary>
    /// El estado que llega en el pedido, o null si no llega (nulo, vacío o solo espacios).
    /// Al crear, null significa Pendiente; al editar, conservar el estado actual.
    /// </summary>
    public static string? EstadoInformado(string? estado)
    {
        return string.IsNullOrWhiteSpace(estado) ? null : estado;
    }
}
