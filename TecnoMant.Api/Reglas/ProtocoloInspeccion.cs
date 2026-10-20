namespace TecnoMant.Api.Reglas;
public static class ProtocoloInspeccion
{
    public static string[] Items(string tipo) => tipo.Trim() switch {
        "Ascensor" => new[] { "Funcionamiento", "Ruidos", "Puertas", "Desgaste de cables", "Iluminación" },
        "Aire acondicionado" => new[] { "Funcionamiento", "Ruidos", "Fugas", "Filtros", "Desgaste" },
        "Activo industrial" => new[] { "Funcionamiento", "Ruidos", "Fugas", "Desgaste", "Protecciones" },
        _ => Array.Empty<string>()
    };
    public static bool Completo(string tipo,IEnumerable<string?> nombres)
    {
        var requeridos=Items(tipo);
        var recibidos=nombres.Select(n=>n?.Trim()??"").ToArray();
        return requeridos.Length>0 && recibidos.Length==requeridos.Length && new HashSet<string>(recibidos,StringComparer.OrdinalIgnoreCase).SetEquals(requeridos);
    }
}
