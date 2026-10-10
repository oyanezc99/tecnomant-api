using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TecnoMant.Api;

/// <summary>
/// Fechas del contrato: ISO 8601 sin zona ni fracción de segundo (2026-10-09T14:30:00).
/// </summary>
public class FechaJsonConverter : JsonConverter<DateTime>
{
    private const string Formato = "yyyy-MM-dd'T'HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTimeOffset(out var fecha))
        {
            // Sin mensaje propio: el serializador añade la ruta del campo (por ejemplo, $.fechaHoraReal).
            throw new JsonException();
        }

        // Se toma la hora tal como viene escrita: si el texto trae zona, no se convierte.
        return fecha.DateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Formato, CultureInfo.InvariantCulture));
    }
}
