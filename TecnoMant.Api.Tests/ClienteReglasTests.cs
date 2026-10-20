using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;
using Xunit;

namespace TecnoMant.Api.Tests;

/// <summary>Campos obligatorios y tamaños de texto de un cliente (FR8.5).</summary>
public class ClienteReglasTests
{
    // Cliente válido base. Cada prueba lo cambia en un solo punto.
    private static Cliente ClienteValido() => new()
    {
        RazonSocial = "Edificio Residencial Los Álamos",
        Direccion = "Av. Arequipa 1234, Lince",
        Contacto = "999000111"
    };

    [Fact]
    public void Validar_ClienteValido_NoDaError()
    {
        var sinContacto = ClienteValido();
        sinContacto.Contacto = null; // contacto es opcional

        var enElLimite = new Cliente
        {
            RazonSocial = new string('a', 150),
            Direccion = new string('a', 250),
            Contacto = new string('a', 150)
        };

        Assert.Null(ClienteReglas.Validar(ClienteValido()));
        Assert.Null(ClienteReglas.Validar(sinContacto));
        Assert.Null(ClienteReglas.Validar(enElLimite));
    }

    [Theory]
    [InlineData(null, "Av. Arequipa 1234, Lince", "razonSocial")]
    [InlineData("   ", "Av. Arequipa 1234, Lince", "razonSocial")]
    [InlineData("Condominio Vista Mar", null, "direccion")]
    [InlineData("Condominio Vista Mar", "", "direccion")]
    public void Validar_SinRazonSocialOSinDireccion_DaError(string? razonSocial, string? direccion, string campoEsperado)
    {
        var cliente = new Cliente { RazonSocial = razonSocial, Direccion = direccion };

        var error = ClienteReglas.Validar(cliente);

        Assert.NotNull(error);
        Assert.StartsWith(campoEsperado + " ", error);
    }

    [Fact]
    public void Validar_TextoMasLargoQueSuColumna_DaError()
    {
        var conRazonSocialLarga = ClienteValido();
        conRazonSocialLarga.RazonSocial = new string('a', 151);

        var conDireccionLarga = ClienteValido();
        conDireccionLarga.Direccion = new string('a', 251);

        var conContactoLargo = ClienteValido();
        conContactoLargo.Contacto = new string('a', 151);

        Assert.Equal("razonSocial admite como máximo 150 caracteres", ClienteReglas.Validar(conRazonSocialLarga));
        Assert.Equal("direccion admite como máximo 250 caracteres", ClienteReglas.Validar(conDireccionLarga));
        Assert.Equal("contacto admite como máximo 150 caracteres", ClienteReglas.Validar(conContactoLargo));
    }
}
