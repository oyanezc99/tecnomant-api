using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;
using Xunit;

namespace TecnoMant.Api.Tests;

/// <summary>Usuario y clave obligatorios (FR2.1) y comparación con el administrador (FR2.2).</summary>
public class LoginReglasTests
{
    // Credenciales inventadas para estas pruebas; no son las de ningún entorno.
    private const string AdminUsuario = "admin";
    private const string AdminClave = "ClaveDePrueba1";

    [Theory]
    [InlineData(null, "1234", "usuario")]
    [InlineData("   ", "1234", "usuario")]
    [InlineData("tec01", null, "clave")]
    [InlineData("tec01", "", "clave")]
    public void Validar_SinUsuarioOSinClave_DaError(string? usuario, string? clave, string campoEsperado)
    {
        var pedido = new LoginPedido { Usuario = usuario, Clave = clave };

        var error = LoginReglas.Validar(pedido);

        Assert.NotNull(error);
        Assert.StartsWith(campoEsperado + " ", error);
    }

    [Fact]
    public void EsAdministrador_ConUsuarioYClaveCorrectos_EsVerdadero()
    {
        // El usuario no distingue mayúsculas: "ADMIN" vale por "admin".
        const string usuario = "ADMIN";
        var pedido = new LoginPedido { Usuario = usuario, Clave = AdminClave };

        var error = LoginReglas.Validar(pedido);
        var esAdministrador = LoginReglas.EsAdministrador(usuario, AdminClave, AdminUsuario, AdminClave);

        Assert.Null(error);
        Assert.True(esAdministrador);
    }

    [Fact]
    public void EsAdministrador_ConLaClaveEnOtrasMayusculas_EsFalso()
    {
        // La clave se compara de forma exacta: "CLAVEDEPRUEBA1" no vale por "ClaveDePrueba1".
        var claveEnMayusculas = AdminClave.ToUpperInvariant();

        var esAdministrador = LoginReglas.EsAdministrador(AdminUsuario, claveEnMayusculas, AdminUsuario, AdminClave);

        Assert.False(esAdministrador);
        Assert.False(LoginReglas.ClaveCoincide(claveEnMayusculas, AdminClave));
    }

    [Fact]
    public void EsAdministrador_SinAdministradorConfigurado_EsFalso()
    {
        // Sin Admin__Usuario o Admin__Clave ningún login coincide, ni siquiera con valores vacíos.
        Assert.False(LoginReglas.EsAdministrador("", "", null, null));
        Assert.False(LoginReglas.EsAdministrador("", "", "", ""));
        Assert.False(LoginReglas.EsAdministrador(AdminUsuario, AdminClave, AdminUsuario, null));
        Assert.False(LoginReglas.EsAdministrador(AdminUsuario, AdminClave, null, AdminClave));
    }
}
