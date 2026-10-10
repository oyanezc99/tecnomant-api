namespace TecnoMant.Api.Modelos;

// Las propiedades de los pedidos son anulables para que el mensaje de cada
// validación lo den las reglas propias (InspeccionReglas), en español.

/// <summary>Pedido de POST /api/inspecciones: una inspección completa.</summary>
public class InspeccionPedido
{
    public int? IdOrden { get; set; }
    public DateTime? FechaHoraReal { get; set; }
    public string? Observaciones { get; set; }
    public List<ItemPedido>? Items { get; set; }
    public List<EvidenciaPedido>? Evidencias { get; set; }
}

/// <summary>Un ítem del checklist dentro del pedido.</summary>
public class ItemPedido
{
    public string? NombreItem { get; set; }

    /// <summary>Bien, Regular o Malo.</summary>
    public string? Resultado { get; set; }

    public string? Observacion { get; set; }
}

/// <summary>Una foto dentro del pedido.</summary>
public class EvidenciaPedido
{
    /// <summary>Solo informativo: el archivo se guarda siempre como {n}.jpg.</summary>
    public string? NombreArchivo { get; set; }

    /// <summary>La foto en Base64, sin el prefijo "data:".</summary>
    public string? FotoBase64 { get; set; }
}

/// <summary>Respuesta 201 de POST /api/inspecciones.</summary>
public class InspeccionCreada
{
    public int IdInspeccion { get; set; }
    public string EstadoGeneral { get; set; } = "";

    /// <summary>Cantidad de fotos guardadas.</summary>
    public int Evidencias { get; set; }
}

/// <summary>Un elemento de GET /api/inspecciones.</summary>
public class InspeccionResumen
{
    public int IdInspeccion { get; set; }
    public int IdOrden { get; set; }
    public DateTime FechaHoraReal { get; set; }
    public string Cliente { get; set; } = "";
    public string EquipoTipo { get; set; } = "";
    public string EquipoCodigo { get; set; } = "";
    public string Tecnico { get; set; } = "";
    public string EstadoGeneral { get; set; } = "";
}

/// <summary>
/// Respuesta de GET /api/inspecciones/{id}: los campos del listado más el detalle,
/// en el orden del contrato.
/// </summary>
public class InspeccionDetalle
{
    public int IdInspeccion { get; set; }
    public int IdOrden { get; set; }
    public DateTime FechaHoraReal { get; set; }
    public string Cliente { get; set; } = "";
    public string EquipoTipo { get; set; } = "";
    public string EquipoCodigo { get; set; } = "";
    public string Tecnico { get; set; } = "";
    public string EstadoGeneral { get; set; } = "";
    public string? Observaciones { get; set; }
    public List<ItemDetalle> Items { get; set; } = new();
    public List<EvidenciaDetalle> Evidencias { get; set; } = new();
}

/// <summary>Un ítem del checklist en el detalle.</summary>
public class ItemDetalle
{
    public string NombreItem { get; set; } = "";
    public string Resultado { get; set; } = "";
    public string? Observacion { get; set; }
}

/// <summary>Una foto en el detalle.</summary>
public class EvidenciaDetalle
{
    public int IdEvidencia { get; set; }

    /// <summary>Ruta de la foto relativa a la URL base, por ejemplo /uploads/inspecciones/12/1.jpg.</summary>
    public string Url { get; set; } = "";

    public DateTime Fecha { get; set; }
}
