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

/// <summary>
/// Orden de trabajo, tal como entra y sale por el mantenimiento /api/ordenes.
/// Al crear y editar, el idOrden y los campos de solo lectura del cuerpo se ignoran.
/// </summary>
public class Orden
{
    public int? IdOrden { get; set; }
    public int? IdEquipo { get; set; }
    public int? IdTecnico { get; set; }
    public DateTime? FechaProgramada { get; set; }

    /// <summary>Pendiente, En proceso o Completada. Si no llega, Pendiente al crear y sin cambio al editar.</summary>
    public string? Estado { get; set; }

    // Solo lectura: los pone la consulta a partir del equipo y del técnico.

    /// <summary>Razón social del cliente del equipo.</summary>
    public string? Cliente { get; set; }

    public string? EquipoTipo { get; set; }
    public string? EquipoCodigo { get; set; }

    /// <summary>Nombre del técnico.</summary>
    public string? Tecnico { get; set; }
}
