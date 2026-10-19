using Dapper;
using Microsoft.Data.SqlClient;
using TecnoMant.Api.Modelos;

namespace TecnoMant.Api.Reglas
{
    public class EquipoReglas
    {
        private readonly string _connectionString;

        public EquipoReglas(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("TecnoMantDB")
                ?? throw new InvalidOperationException("Falta la cadena de conexión TecnoMantDB.");
        }

        public async Task<IEnumerable<Equipo>> ObtenerTodosAsync()
        {
            using var db = new SqlConnection(_connectionString);
            var sql = @"SELECT e.IdEquipo, e.IdCliente, e.Tipo, e.CodigoSerie, e.Ubicacion, c.RazonSocial AS ClienteRazonSocial 
                        FROM Equipo e 
                        INNER JOIN Cliente c ON e.IdCliente = c.IdCliente";
            return await db.QueryAsync<Equipo>(sql);
        }

        public async Task<Equipo?> ObtenerPorIdAsync(int id)
        {
            using var db = new SqlConnection(_connectionString);
            var sql = @"SELECT e.IdEquipo, e.IdCliente, e.Tipo, e.CodigoSerie, e.Ubicacion, c.RazonSocial AS ClienteRazonSocial 
                        FROM Equipo e 
                        INNER JOIN Cliente c ON e.IdCliente = c.IdCliente 
                        WHERE e.IdEquipo = @Id";
            return await db.QueryFirstOrDefaultAsync<Equipo>(sql, new { Id = id });
        }

        public async Task<int> CrearAsync(DatosEquipo datos)
        {
            using var db = new SqlConnection(_connectionString);
            var sql = @"INSERT INTO Equipo (IdCliente, Tipo, CodigoSerie, Ubicacion) 
                        OUTPUT INSERTED.IdEquipo 
                        VALUES (@IdCliente, @Tipo, @CodigoSerie, @Ubicacion)";
            return await db.ExecuteScalarAsync<int>(sql, datos);
        }

        public async Task<bool> ActualizarAsync(int id, DatosEquipo datos)
        {
            using var db = new SqlConnection(_connectionString);
            var sql = @"UPDATE Equipo 
                        SET IdCliente = @IdCliente, Tipo = @Tipo, CodigoSerie = @CodigoSerie, Ubicacion = @Ubicacion 
                        WHERE IdEquipo = @Id";
            var filasAfectadas = await db.ExecuteAsync(sql, new { Id = id, datos.IdCliente, datos.Tipo, datos.CodigoSerie, datos.Ubicacion });
            return filasAfectadas > 0;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            using var db = new SqlConnection(_connectionString);
            var sql = "DELETE FROM Equipo WHERE IdEquipo = @Id";
            var filasAfectadas = await db.ExecuteAsync(sql, new { Id = id });
            return filasAfectadas > 0;
        }
    }
}