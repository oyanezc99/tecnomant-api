using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
namespace TecnoMant.Api;

/// <summary>Sesiones locales con token opaco aleatorio. Reiniciar el servidor exige iniciar sesión de nuevo.</summary>
public sealed class SesionTokens
{
    private readonly ConcurrentDictionary<string, (ClaimsPrincipal Usuario, DateTime Vence)> sesiones = new();
    public string Crear(string rol, string nombre, int? id)
    {
        foreach(var anterior in sesiones.Where(s=>s.Value.Vence<=DateTime.UtcNow)) sesiones.TryRemove(anterior.Key,out _);
        var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var identidad=new ClaimsIdentity(new [] { new Claim(ClaimTypes.Role,rol), new Claim(ClaimTypes.Name,nombre), new Claim(ClaimTypes.NameIdentifier,id?.ToString()??"admin") },"Bearer");
        sesiones[token]=(new ClaimsPrincipal(identidad),DateTime.UtcNow.AddHours(12));
        return token;
    }
    public ClaimsPrincipal? Leer(string token)
    {
        if(sesiones.TryGetValue(token,out var sesion) && sesion.Vence>DateTime.UtcNow) return sesion.Usuario;
        sesiones.TryRemove(token,out _); return null;
    }
    public void Cerrar(string token) => sesiones.TryRemove(token,out _);
}
