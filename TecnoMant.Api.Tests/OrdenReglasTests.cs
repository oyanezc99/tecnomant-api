using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;
using Xunit;

namespace TecnoMant.Api.Tests;

/// <summary>
/// Campos obligatorios, rango de fecha y estados permitidos de una orden,
/// y el estado que no llega, Pendiente al crear y sin cambio al editar.
/// </summary>
public class OrdenReglasTests
{
    // Orden válida base (como la orden 1 de 02_seed.sql). Cada prueba la cambia en un solo punto.
    private static Orden OrdenValida() => new()
    {
        IdEquipo = 1,
        IdTecnico = 1,
        FechaProgramada = new DateTime(2026, 10, 20, 9, 0, 0),
        Estado = "Pendiente"
    };

    [Fact]
    public void Validar_OrdenValida_NoDaError()
    {
        var sinEstado = OrdenValida();
        sinEstado.Estado = null; // estado es opcional

        var conEstadoVacio = OrdenValida();
        conEstadoVacio.Estado = "";

        // Los extremos del rango de la columna DATETIME se aceptan.
        var enLaFechaMinima = OrdenValida();
        enLaFechaMinima.FechaProgramada = new DateTime(1753, 1, 1);

        var enLaFechaMaxima = OrdenValida();
        enLaFechaMaxima.FechaProgramada = new DateTime(9999, 12, 31, 23, 59, 59);

        Assert.Null(OrdenReglas.Validar(OrdenValida()));
        Assert.Null(OrdenReglas.Validar(sinEstado));
        Assert.Null(OrdenReglas.Validar(conEstadoVacio));
        Assert.Null(OrdenReglas.Validar(enLaFechaMinima));
        Assert.Null(OrdenReglas.Validar(enLaFechaMaxima));
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("En proceso")]
    [InlineData("Completada")]
    public void Validar_EstadoPermitido_NoDaError(string estado)
    {
        var orden = OrdenValida();
        orden.Estado = estado;

        Assert.Null(OrdenReglas.Validar(orden));
    }

    [Theory]
    [InlineData(null, 1, true, "idEquipo")]
    [InlineData(1, null, true, "idTecnico")]
    [InlineData(1, 1, false, "fechaProgramada")]
    public void Validar_SinCampoObligatorio_DaError(int? idEquipo, int? idTecnico, bool conFecha, string campoEsperado)
    {
        var orden = new Orden
        {
            IdEquipo = idEquipo,
            IdTecnico = idTecnico,
            FechaProgramada = conFecha ? new DateTime(2026, 10, 20, 9, 0, 0) : null
        };

        var error = OrdenReglas.Validar(orden);

        Assert.NotNull(error);
        Assert.StartsWith(campoEsperado + " ", error);
    }

    [Theory]
    [InlineData("Cancelada")]
    [InlineData("pendiente")] // debe ser exacto: en minúsculas no vale
    public void Validar_EstadoNoPermitido_DaError(string estado)
    {
        var orden = OrdenValida();
        orden.Estado = estado;

        var error = OrdenReglas.Validar(orden);

        Assert.Equal("estado debe ser Pendiente, En proceso o Completada", error);
    }

    [Fact]
    public void Validar_FechaFueraDelRangoDeDatetime_DaError()
    {
        // La columna DATETIME no admite fechas anteriores al 1753-01-01: sin esta regla la base daría un 500.
        var en1700 = OrdenValida();
        en1700.FechaProgramada = new DateTime(1700, 1, 1);

        var unSegundoAntes = OrdenValida();
        unSegundoAntes.FechaProgramada = new DateTime(1752, 12, 31, 23, 59, 59);

        var despuesDelMaximo = OrdenValida();
        despuesDelMaximo.FechaProgramada = DateTime.MaxValue;

        const string mensaje = "fechaProgramada debe estar entre 1753-01-01 y 9999-12-31";
        Assert.Equal(mensaje, OrdenReglas.Validar(en1700));
        Assert.Equal(mensaje, OrdenReglas.Validar(unSegundoAntes));
        Assert.Equal(mensaje, OrdenReglas.Validar(despuesDelMaximo));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("En proceso", "En proceso")]
    public void EstadoInformado_SegunElEstadoQueLlega_DevuelveElEstadoONull(string? estado, string? esperado)
    {
        Assert.Equal(esperado, OrdenReglas.EstadoInformado(estado));
    }
}
