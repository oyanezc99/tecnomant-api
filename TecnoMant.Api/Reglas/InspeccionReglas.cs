using TecnoMant.Api.Modelos;

namespace TecnoMant.Api.Reglas;

/// <summary>Validaciones y cálculos de una inspección que no necesitan la base de datos.</summary>
public static class InspeccionReglas
{
    // Resultados permitidos de un ítem (RN-05); también son los estados generales.
    public const string Bien = "Bien";
    public const string Regular = "Regular";
    public const string Malo = "Malo";

    public const int MaximoFotos = 5;
    public const int MaximoBytesFoto = 2 * 1024 * 1024; // 2 MB, ya decodificada

    // Tamaños de las columnas de texto (01_schema.sql).
    public const int MaximoObservaciones = 1000;  // Inspeccion.Observaciones
    public const int MaximoNombreItem = 120;      // ItemChecklist.NombreItem
    public const int MaximoObservacionItem = 500; // ItemChecklist.Observacion

    // Caracteres Base64 que ocupan 2 MB: 4 por cada 3 bytes, redondeando hacia arriba.
    private const int MaximoCaracteresBase64 = (MaximoBytesFoto + 2) / 3 * 4;

    /// <summary>
    /// Valida el pedido sin tocar la base de datos. Devuelve el primer mensaje de error,
    /// o null si el pedido es válido; en ese caso <paramref name="fotos"/> trae las fotos
    /// ya decodificadas, en el orden del pedido. Con error, la lista queda vacía.
    /// </summary>
    public static string? Validar(InspeccionPedido pedido, out List<byte[]> fotos)
    {
        fotos = new List<byte[]>();
        var items = pedido.Items ?? new List<ItemPedido>();
        var evidencias = pedido.Evidencias ?? new List<EvidenciaPedido>();

        if (pedido.IdOrden is null)
        {
            return "idOrden es obligatorio";
        }

        if (pedido.FechaHoraReal is null)
        {
            return "fechaHoraReal es obligatoria";
        }

        // Cada grupo de reglas se revisa solo si el anterior no encontró errores.
        var error = ValidarItems(items)
            ?? ValidarFotos(items, evidencias, fotos)
            ?? ValidarLongitudes(pedido.Observaciones, items);

        if (error is not null)
        {
            fotos.Clear();
        }

        return error;
    }

    /// <summary>
    /// Estado general de la inspección: Malo si algún ítem está en Malo (RN-08);
    /// si no, Regular si alguno está en Regular; si no, Bien.
    /// </summary>
    public static string CalcularEstadoGeneral(IEnumerable<ItemPedido> items)
    {
        var resultados = items.Select(item => item.Resultado).ToList();

        if (resultados.Contains(Malo))
        {
            return Malo;
        }

        if (resultados.Contains(Regular))
        {
            return Regular;
        }

        return Bien;
    }

    private static string? ValidarItems(List<ItemPedido> items)
    {
        if (items.Count == 0)
        {
            return "items debe traer al menos 1 ítem";
        }

        for (var i = 0; i < items.Count; i++)
        {
            // Un elemento nulo en la lista cuenta como ítem sin nombre.
            if (string.IsNullOrWhiteSpace(items[i]?.NombreItem))
            {
                return $"nombreItem es obligatorio (ítem {i + 1})";
            }
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Resultado is not (Bien or Regular or Malo))
            {
                return $"resultado debe ser Bien, Regular o Malo (ítem {i + 1})";
            }
        }

        for (var i = 0; i < items.Count; i++)
        {
            // RN-01: un ítem en Malo exige observación.
            if (items[i].Resultado == Malo && string.IsNullOrWhiteSpace(items[i].Observacion))
            {
                return $"observacion es obligatoria cuando el resultado es Malo (ítem {i + 1})";
            }
        }

        return null;
    }

    private static string? ValidarFotos(List<ItemPedido> items, List<EvidenciaPedido> evidencias, List<byte[]> fotos)
    {
        // RN-01: con algún ítem en Malo, la inspección debe traer al menos 1 foto.
        if (evidencias.Count == 0 && items.Any(item => item.Resultado == Malo))
        {
            return "evidencias debe traer al menos 1 foto cuando algún ítem está en Malo";
        }

        if (evidencias.Count > MaximoFotos)
        {
            return $"evidencias admite como máximo {MaximoFotos} fotos";
        }

        for (var i = 0; i < evidencias.Count; i++)
        {
            // Un elemento nulo en la lista cuenta como foto vacía.
            var error = DecodificarFoto(evidencias[i]?.FotoBase64, i + 1, out var foto);
            if (error is not null)
            {
                return error;
            }

            fotos.Add(foto);
        }

        return null;
    }

    private static string? DecodificarFoto(string? base64, int numero, out byte[] foto)
    {
        foto = Array.Empty<byte>();
        var demasiadoGrande = $"fotoBase64 supera el máximo de 2 MB (foto {numero})";

        if (string.IsNullOrWhiteSpace(base64))
        {
            return $"fotoBase64 es obligatoria (foto {numero})";
        }

        // Antes de decodificar: un texto que ya no cabe en 2 MB se rechaza sin reservar memoria.
        // Los espacios y saltos de línea no cuentan, porque el decodificador los ignora.
        if (base64.Count(caracter => !char.IsWhiteSpace(caracter)) > MaximoCaracteresBase64)
        {
            return demasiadoGrande;
        }

        try
        {
            foto = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            // El texto no es Base64: se informa como error de validación (400).
            return $"fotoBase64 debe ser Base64 válido, sin el prefijo data: (foto {numero})";
        }

        // El límite exacto se comprueba con la foto ya decodificada.
        if (foto.Length > MaximoBytesFoto)
        {
            return demasiadoGrande;
        }

        return null;
    }

    private static string? ValidarLongitudes(string? observaciones, List<ItemPedido> items)
    {
        if (observaciones is { Length: > MaximoObservaciones })
        {
            return $"observaciones admite como máximo {MaximoObservaciones} caracteres";
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].NombreItem is { Length: > MaximoNombreItem })
            {
                return $"nombreItem admite como máximo {MaximoNombreItem} caracteres (ítem {i + 1})";
            }

            if (items[i].Observacion is { Length: > MaximoObservacionItem })
            {
                return $"observacion admite como máximo {MaximoObservacionItem} caracteres (ítem {i + 1})";
            }
        }

        return null;
    }
}
