using TecnoMant.Api.Modelos;

namespace TecnoMant.Api.Reglas;

/// <summary>Reglas del login que no necesitan la base de datos.</summary>
public static class LoginReglas
{
    /// <summary>Devuelve el mensaje de error, o null si el pedido trae usuario y clave.</summary>
    public static string? Validar(LoginPedido pedido)
    {
        if (string.IsNullOrWhiteSpace(pedido.Usuario))
        {
            return "usuario es obligatorio";
        }

        // La clave no se recorta: se compara tal como llega.
        if (string.IsNullOrEmpty(pedido.Clave))
        {
            return "clave es obligatoria";
        }

        return null;
    }

    /// <summary>
    /// Compara con el administrador de la configuración (Admin:Usuario y Admin:Clave).
    /// Si el administrador no está configurado, ningún login coincide con él:
    /// un valor vacío no sirve como credencial.
    /// </summary>
    public static bool EsAdministrador(string usuario, string clave, string? adminUsuario, string? adminClave)
    {
        if (string.IsNullOrWhiteSpace(adminUsuario) || string.IsNullOrEmpty(adminClave))
        {
            return false;
        }

        // El usuario no distingue mayúsculas; la clave sí.
        return string.Equals(usuario, adminUsuario.Trim(), StringComparison.OrdinalIgnoreCase)
            && ClaveCoincide(clave, adminClave);
    }

    /// <summary>Compara la clave de forma exacta, distinguiendo mayúsculas y minúsculas.</summary>
    public static bool ClaveCoincide(string claveEnviada, string claveGuardada)
    {
        return string.Equals(claveEnviada, claveGuardada, StringComparison.Ordinal);
    }
}
