using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;
using Xunit;

namespace TecnoMant.Api.Tests;

/// <summary>
/// Campos obligatorios, dni y tamaños de texto de un técnico,
/// y la clave vacía que en la edición significa "no cambiar".
/// </summary>
public class TecnicoReglasTests
{
    // Técnico válido base (como los de 02_seed.sql). Cada prueba lo cambia en un solo punto.
    private static TecnicoPedido TecnicoValido() => new()
    {
        Nombre = "Tecnico Prueba 3",
        Dni = "10000003",
        Especialidad = "Ascensores",
        Usuario = "tec03",
        Clave = "1234"
    };

    [Fact]
    public void ValidarAlta_TecnicoValido_NoDaError()
    {
        var sinEspecialidad = TecnicoValido();
        sinEspecialidad.Especialidad = null; // especialidad es opcional

        // dni se mide sin los espacios de los extremos, que no se guardan.
        var conDniConEspacios = TecnicoValido();
        conDniConEspacios.Dni = " 10000003 ";

        var enElLimite = new TecnicoPedido
        {
            Nombre = new string('a', 120),
            Dni = "10000003",
            Especialidad = new string('a', 60),
            Usuario = new string('a', 50),
            Clave = new string('a', 50)
        };

        Assert.Null(TecnicoReglas.ValidarAlta(TecnicoValido()));
        Assert.Null(TecnicoReglas.ValidarAlta(sinEspecialidad));
        Assert.Null(TecnicoReglas.ValidarAlta(conDniConEspacios));
        Assert.Null(TecnicoReglas.ValidarAlta(enElLimite));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidarEdicion_SinClave_NoDaError(string? clave)
    {
        var pedido = TecnicoValido();
        pedido.Clave = clave;

        Assert.Null(TecnicoReglas.ValidarEdicion(pedido));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidarAlta_SinClave_DaError(string? clave)
    {
        var pedido = TecnicoValido();
        pedido.Clave = clave;

        Assert.Equal("clave es obligatoria", TecnicoReglas.ValidarAlta(pedido));
    }

    [Theory]
    [InlineData("1234567")]   // 7 dígitos
    [InlineData("123456789")] // 9 dígitos
    [InlineData("1234567A")]  // una letra
    [InlineData("١٢٣٤٥٦٧٨")]  // dígitos de otro alfabeto: solo valen del 0 al 9
    public void ValidarAlta_DniQueNoSonOchoDigitos_DaError(string dni)
    {
        var pedido = TecnicoValido();
        pedido.Dni = dni;

        Assert.Equal("dni debe tener exactamente 8 dígitos", TecnicoReglas.ValidarAlta(pedido));
    }

    [Theory]
    [InlineData(null, "10000003", "tec03", "nombre")]
    [InlineData("   ", "10000003", "tec03", "nombre")]
    [InlineData("Tecnico Prueba 3", null, "tec03", "dni")]
    [InlineData("Tecnico Prueba 3", "10000003", null, "usuario")]
    [InlineData("Tecnico Prueba 3", "10000003", "", "usuario")]
    public void ValidarEdicion_SinCampoObligatorio_DaError(string? nombre, string? dni, string? usuario, string campoEsperado)
    {
        var pedido = new TecnicoPedido { Nombre = nombre, Dni = dni, Usuario = usuario };

        var error = TecnicoReglas.ValidarEdicion(pedido);

        Assert.NotNull(error);
        Assert.StartsWith(campoEsperado + " ", error);
    }

    [Fact]
    public void ValidarAlta_TextoMasLargoQueSuColumna_DaError()
    {
        var conNombreLargo = TecnicoValido();
        conNombreLargo.Nombre = new string('a', 121);

        var conEspecialidadLarga = TecnicoValido();
        conEspecialidadLarga.Especialidad = new string('a', 61);

        var conUsuarioLargo = TecnicoValido();
        conUsuarioLargo.Usuario = new string('a', 51);

        var conClaveLarga = TecnicoValido();
        conClaveLarga.Clave = new string('a', 51);

        Assert.Equal("nombre admite como máximo 120 caracteres", TecnicoReglas.ValidarAlta(conNombreLargo));
        Assert.Equal("especialidad admite como máximo 60 caracteres", TecnicoReglas.ValidarAlta(conEspecialidadLarga));
        Assert.Equal("usuario admite como máximo 50 caracteres", TecnicoReglas.ValidarAlta(conUsuarioLargo));
        Assert.Equal("clave admite como máximo 50 caracteres", TecnicoReglas.ValidarAlta(conClaveLarga));

        // En la edición, una clave nueva también debe caber en su columna.
        Assert.Equal("clave admite como máximo 50 caracteres", TecnicoReglas.ValidarEdicion(conClaveLarga));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("1234", false)]
    public void ClaveSinCambio_SegunLaClaveQueLlega_DiceSiSeConserva(string? clave, bool esperado)
    {
        Assert.Equal(esperado, TecnicoReglas.ClaveSinCambio(clave));
    }
}
