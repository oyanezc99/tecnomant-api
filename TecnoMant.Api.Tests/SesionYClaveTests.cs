using TecnoMant.Api;
using Xunit;
namespace TecnoMant.Api.Tests;
public class SesionYClaveTests
{
    [Fact] public void UnaClaveSeGuardaConSalYSeValidaSinDevolverElTexto()
    {
        var hash=ClaveSegura.Crear("Prueba-segura");
        Assert.NotEqual("Prueba-segura",hash);
        Assert.NotEqual(hash,ClaveSegura.Crear("Prueba-segura"));
        Assert.True(ClaveSegura.Verificar("Prueba-segura",hash));
        Assert.False(ClaveSegura.Verificar("otra",hash));
        Assert.False(ClaveSegura.Verificar("Prueba-segura","pbkdf2$invalido"));
    }
    [Fact] public void LaSesionConservaRolYSePuedeRevocar()
    {
        var sesiones=new SesionTokens();
        var token=sesiones.Crear("tecnico","Prueba",10);
        Assert.True(sesiones.Leer(token)!.IsInRole("tecnico"));
        Assert.Null(sesiones.Leer("token-inventado"));
        sesiones.Cerrar(token);
        Assert.Null(sesiones.Leer(token));
    }
}
