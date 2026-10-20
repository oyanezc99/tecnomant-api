using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace TecnoMant.Api.Controllers;

[ApiController]
[Route("api/ordenes")]
public class OrdenesAdministracionController(SqlConnection db) : ControllerBase
{
    private const string Consulta = """
        SELECT o.IdOrden, o.IdEquipo, o.IdTecnico, t.Nombre AS Tecnico,
               c.RazonSocial AS Cliente, c.Direccion, e.Tipo AS EquipoTipo,
               e.CodigoSerie AS EquipoCodigo, e.Ubicacion, o.FechaProgramada, o.Estado
        FROM OrdenTrabajo o JOIN Equipo e ON e.IdEquipo=o.IdEquipo
        JOIN Cliente c ON c.IdCliente=e.IdCliente JOIN Tecnico t ON t.IdTecnico=o.IdTecnico
        """;

    [HttpGet]
    public async Task<IActionResult> Listar(int? idEquipo, int? idTecnico) => Ok(await db.QueryAsync<OrdenAdministrativa>(
        Consulta + " WHERE (@idEquipo IS NULL OR o.IdEquipo=@idEquipo) AND (@idTecnico IS NULL OR o.IdTecnico=@idTecnico) ORDER BY o.FechaProgramada, o.IdOrden", new { idEquipo, idTecnico }));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var orden = await db.QuerySingleOrDefaultAsync<OrdenAdministrativa>(Consulta + " WHERE o.IdOrden=@id", new { id });
        return orden is null ? NotFound(new { mensaje="La orden no existe" }) : Ok(orden);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(PedidoOrden pedido)
    {
        var error = await Validar(pedido);
        if (error is not null) return BadRequest(new { mensaje=error });
        var id = await db.ExecuteScalarAsync<int>("""
            INSERT OrdenTrabajo (IdEquipo,IdTecnico,FechaProgramada,Estado)
            OUTPUT INSERTED.IdOrden VALUES (@IdEquipo,@IdTecnico,@FechaProgramada,@Estado)
            """, pedido);
        return CreatedAtAction(nameof(Obtener), new { id }, new { idOrden=id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, PedidoOrden pedido)
    {
        var error = await Validar(pedido);
        if (error is not null) return BadRequest(new { mensaje=error });
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync();
        var estado = await db.QuerySingleOrDefaultAsync<string>("SELECT Estado FROM OrdenTrabajo WITH (UPDLOCK,HOLDLOCK) WHERE IdOrden=@id",new { id },tx);
        if (estado is null) return NotFound(new { mensaje="La orden no existe" });
        if (estado=="Completada" || await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Inspeccion WHERE IdOrden=@id",new { id },tx)>0)
            return Conflict(new { mensaje="Una orden con inspección no se puede modificar" });
        await db.ExecuteAsync("UPDATE OrdenTrabajo SET IdEquipo=@IdEquipo,IdTecnico=@IdTecnico,FechaProgramada=@FechaProgramada,Estado=@Estado WHERE IdOrden=@id",new { id,pedido.IdEquipo,pedido.IdTecnico,pedido.FechaProgramada,pedido.Estado },tx);
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try {
            var filas = await db.ExecuteAsync("DELETE FROM OrdenTrabajo WHERE IdOrden=@id",new { id });
            return filas==0 ? NotFound(new { mensaje="La orden no existe" }) : NoContent();
        } catch (SqlException e) when(e.Number==547) {
            return Conflict(new { mensaje="No se puede eliminar una orden que ya tiene inspección" });
        }
    }

    private async Task<string?> Validar(PedidoOrden p)
    {
        if (p.IdEquipo<=0 || p.IdTecnico<=0 || p.FechaProgramada is null) return "Selecciona equipo, técnico y fecha programada";
        if (p.Estado is not ("Pendiente" or "En proceso")) return "El estado debe ser Pendiente o En proceso; la inspección completa la orden";
        if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Equipo WHERE IdEquipo=@IdEquipo",p)==0) return "El equipo no existe";
        if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Tecnico WHERE IdTecnico=@IdTecnico",p)==0) return "El técnico no existe";
        return null;
    }
}

public class PedidoOrden
{
    public int IdEquipo {get;set;}
    public int IdTecnico {get;set;}
    public DateTime? FechaProgramada {get;set;}
    public string Estado {get;set;} = "Pendiente";
}

public class OrdenAdministrativa : TecnoMant.Api.Modelos.OrdenTecnico { public int IdEquipo {get;set;} public int IdTecnico {get;set;} public string Tecnico {get;set;} = string.Empty; }
