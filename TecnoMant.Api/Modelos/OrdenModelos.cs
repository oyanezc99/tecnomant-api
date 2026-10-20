namespace TecnoMant.Api.Modelos;

/// <summary>Una orden de GET /api/ordenes/tecnico/{idTecnico}.</summary>
public class OrdenTecnico
{
    public int IdOrden { get; set; }

    /// <summary>Razón social del cliente.</summary>
    public string Cliente { get; set; } = "";

    public string Direccion { get; set; } = "";
    public string EquipoTipo { get; set; } = "";
    public string EquipoCodigo { get; set; } = "";
    public string? Ubicacion { get; set; }
    public DateTime FechaProgramada { get; set; }
    public string Estado { get; set; } = "";
}

/// <summary>Respuesta de GET /api/ordenes/resumen: número de órdenes por estado.</summary>
public class ResumenOrdenes
{
    public int Pendientes { get; set; }
    public int EnProceso { get; set; }
    public int Completadas { get; set; }
}
