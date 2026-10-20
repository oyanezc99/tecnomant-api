namespace TecnoMant.Api.Modelos
{
    public class Equipo
    {
        public int IdEquipo { get; set; }
        public int IdCliente { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string CodigoSerie { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        // Opcional para mostrar la razón social del cliente al listar equipos
        public string? ClienteRazonSocial { get; set; }
    }

    public class DatosEquipo
    {
        public int IdCliente { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string CodigoSerie { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
    }
}