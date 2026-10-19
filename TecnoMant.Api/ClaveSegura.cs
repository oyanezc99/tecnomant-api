using System.Security.Cryptography;
namespace TecnoMant.Api;

public static class ClaveSegura
{
    public static string Crear(string clave)
    {
        var sal=RandomNumberGenerator.GetBytes(16);
        var hash=Rfc2898DeriveBytes.Pbkdf2(clave,sal,100000,HashAlgorithmName.SHA256,32);
        return "pbkdf2$100000$"+Convert.ToBase64String(sal)+"$"+Convert.ToBase64String(hash);
    }
    public static bool Verificar(string clave,string guardada)
    {
        if(!guardada.StartsWith("pbkdf2$")) return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(clave),System.Text.Encoding.UTF8.GetBytes(guardada));
        try {
            var p=guardada.Split('$');
            var vueltas=int.Parse(p[1]);
            if(vueltas<100000 || vueltas>1000000) return false;
            var hash=Rfc2898DeriveBytes.Pbkdf2(clave,Convert.FromBase64String(p[2]),vueltas,HashAlgorithmName.SHA256,32);
            return CryptographicOperations.FixedTimeEquals(hash,Convert.FromBase64String(p[3]));
        } catch(FormatException) {return false;} catch(IndexOutOfRangeException) {return false;}
    }
}
