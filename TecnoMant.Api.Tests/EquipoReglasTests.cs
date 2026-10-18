using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;
using Xunit;

namespace TecnoMant.Api.Tests;

/// <summary>Campos obligatorios, tipos permitidos y tamaños de texto de un equipo.</summary>
public class EquipoReglasTests
{
    // Equipo válido base (el equipo 1 de 02_seed.sql). Cada prueba lo cambia en un solo punto.
    private static Equipo EquipoValido() => new()
    {
        IdCliente = 1,
        Tipo = "Ascensor",
        CodigoSerie = "ASC-001",
        Ubicacion = "Torre A"
    };

    [Fact]
    public void Validar_EquipoValido_NoDaError()
    {
        var sinUbicacion = EquipoValido();
        sinUbicacion.Ubicacion = null; // ubicacion es opcional

        var enElLimite = EquipoValido();
        enElLimite.CodigoSerie = new string('a', 60);
        enElLimite.Ubicacion = new string('a', 200);

        // codigoSerie se mide sin los espacios de los extremos, que no se guardan.
        var conEspaciosSobrantes = EquipoValido();
        conEspaciosSobrantes.CodigoSerie = "  " + new string('a', 60) + "  ";

        Assert.Null(EquipoReglas.Validar(EquipoValido()));
        Assert.Null(EquipoReglas.Validar(sinUbicacion));
        Assert.Null(EquipoReglas.Validar(enElLimite));
        Assert.Null(EquipoReglas.Validar(conEspaciosSobrantes));
    }

    [Theory]
    [InlineData("Ascensor")]
    [InlineData("Aire acondicionado")]
    [InlineData("Activo industrial")]
    public void Validar_TipoPermitido_NoDaError(string tipo)
    {
        var equipo = EquipoValido();
        equipo.Tipo = tipo;

        Assert.Null(EquipoReglas.Validar(equipo));
    }

    [Theory]
    [InlineData(null, "Ascensor", "ASC-001", "idCliente")]
    [InlineData(1, "Ascensor", null, "codigoSerie")]
    [InlineData(1, "Ascensor", "", "codigoSerie")]
    public void Validar_SinCampoObligatorio_DaError(int? idCliente, string? tipo, string? codigoSerie, string campoEsperado)
    {
        var equipo = new Equipo { IdCliente = idCliente, Tipo = tipo, CodigoSerie = codigoSerie };

        var error = EquipoReglas.Validar(equipo);

        Assert.NotNull(error);
        Assert.StartsWith(campoEsperado + " ", error);
    }

    // Mensaje exacto: "tipo debe ser ..." también empieza por "tipo ", así que StartsWith
    // no distinguiría un tipo que falta de uno no permitido.
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validar_SinTipo_DaError(string? tipo)
    {
        var equipo = EquipoValido();
        equipo.Tipo = tipo;

        Assert.Equal("tipo es obligatorio", EquipoReglas.Validar(equipo));
    }

    [Theory]
    [InlineData("Montacargas")]
    [InlineData("ascensor")] // debe ser exacto: en minúsculas no vale
    public void Validar_TipoNoPermitido_DaError(string tipo)
    {
        var equipo = EquipoValido();
        equipo.Tipo = tipo;

        var error = EquipoReglas.Validar(equipo);

        Assert.Equal("tipo debe ser Ascensor, Aire acondicionado o Activo industrial", error);
    }

    [Fact]
    public void Validar_TextoMasLargoQueSuColumna_DaError()
    {
        var conCodigoSerieLargo = EquipoValido();
        conCodigoSerieLargo.CodigoSerie = new string('a', 61);

        var conUbicacionLarga = EquipoValido();
        conUbicacionLarga.Ubicacion = new string('a', 201);

        Assert.Equal("codigoSerie admite como máximo 60 caracteres", EquipoReglas.Validar(conCodigoSerieLargo));
        Assert.Equal("ubicacion admite como máximo 200 caracteres", EquipoReglas.Validar(conUbicacionLarga));
    }
}
