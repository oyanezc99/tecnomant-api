namespace TecnoMant.Api.Modelos
{
    public class Tecnico
    {
        public int IdTecnico { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
    }

    public class DatosTecnico
    {
        public string Nombre { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
    }
}