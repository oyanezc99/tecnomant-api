using System.Diagnostics.CodeAnalysis;
using TecnoMant.Api.Modelos;

namespace TecnoMant.Api.Reglas;

/// <summary>Validaciones de un técnico que no necesitan la base de datos.</summary>
public static class TecnicoReglas
{
    // Tamaños de las columnas de la tabla Tecnico (01_schema.sql).
    public const int MaximoNombre = 120;
    public const int LargoDni = 8;
    public const int MaximoEspecialidad = 60;
    public const int MaximoUsuario = 50;
    public const int MaximoClave = 50;

    /// <summary>
    /// Validaciones del alta: la clave es obligatoria.
    /// Devuelve el primer mensaje de error, o null si el pedido es válido.
    /// </summary>
    public static string? ValidarAlta(TecnicoPedido pedido)
    {
        return Validar(pedido, claveObligatoria: true);
    }

    /// <summary>
    /// Validaciones de la edición: sin clave, la clave guardada no cambia.
    /// Devuelve el primer mensaje de error, o null si el pedido es válido.
    /// </summary>
    public static string? ValidarEdicion(TecnicoPedido pedido)
    {
        return Validar(pedido, claveObligatoria: false);
    }

    /// <summary>
    /// true si la clave no llega: nula, vacía o solo espacios. En la edición significa
    /// "no cambiar la clave"; en el alta, que falta.
    /// </summary>
    public static bool ClaveSinCambio([NotNullWhen(false)] string? clave)
    {
        return string.IsNullOrWhiteSpace(clave);
    }

    // nombre, dni y usuario se miden sin espacios sobrantes, que es como se guardan.
    // La clave se mide y se guarda tal cual, porque el login la compara de forma exacta.
    private static string? Validar(TecnicoPedido pedido, bool claveObligatoria)
    {
        if (string.IsNullOrWhiteSpace(pedido.Nombre))
        {
            return "nombre es obligatorio";
        }

        if (pedido.Nombre.Trim().Length > MaximoNombre)
        {
            return $"nombre admite como máximo {MaximoNombre} caracteres";
        }

        if (string.IsNullOrWhiteSpace(pedido.Dni))
        {
            return "dni es obligatorio";
        }

        if (!EsDniValido(pedido.Dni.Trim()))
        {
            return $"dni debe tener exactamente {LargoDni} dígitos";
        }

        if (pedido.Especialidad is not null && pedido.Especialidad.Length > MaximoEspecialidad)
        {
            return $"especialidad admite como máximo {MaximoEspecialidad} caracteres";
        }

        if (string.IsNullOrWhiteSpace(pedido.Usuario))
        {
            return "usuario es obligatorio";
        }

        if (pedido.Usuario.Trim().Length > MaximoUsuario)
        {
            return $"usuario admite como máximo {MaximoUsuario} caracteres";
        }

        if (claveObligatoria && ClaveSinCambio(pedido.Clave))
        {
            return "clave es obligatoria";
        }

        if (!ClaveSinCambio(pedido.Clave) && pedido.Clave.Length > MaximoClave)
        {
            return $"clave admite como máximo {MaximoClave} caracteres";
        }

        return null;
    }

    // Solo dígitos del 0 al 9: char.IsDigit también aceptaría dígitos de otros alfabetos,
    // que no caben en la columna CHAR(8).
    private static bool EsDniValido(string dni)
    {
        return dni.Length == LargoDni && dni.All(char.IsAsciiDigit);
    }
}
