using System.Text.Json;
using Xunit;

namespace TecnoMant.Api.Tests;

/// <summary>Fechas ISO 8601 sin zona ni fracción de segundo.</summary>
public class FechaJsonConverterTests
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        Converters = { new FechaJsonConverter() }
    };

    [Fact]
    public void Escribe_UnaFechaConMilisegundos_SinZonaNiFraccion()
    {
        var conMilisegundos = new DateTime(2026, 10, 9, 14, 30, 0, 123);
        var enUtc = new DateTime(2026, 10, 9, 14, 30, 0, 999, DateTimeKind.Utc);

        var jsonConMilisegundos = JsonSerializer.Serialize(conMilisegundos, Opciones);
        var jsonEnUtc = JsonSerializer.Serialize(enUtc, Opciones);

        // Sin ".123" y sin la "Z" que el formato por defecto pone a una fecha UTC.
        Assert.Equal("\"2026-10-09T14:30:00\"", jsonConMilisegundos);
        Assert.Equal("\"2026-10-09T14:30:00\"", jsonEnUtc);
    }

    [Fact]
    public void Lee_UnaFechaIsoSinZona()
    {
        var fecha = JsonSerializer.Deserialize<DateTime>("\"2026-10-09T14:30:00\"", Opciones);

        Assert.Equal(new DateTime(2026, 10, 9, 14, 30, 0), fecha);
    }
}
