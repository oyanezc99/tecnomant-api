using Dapper;
using Microsoft.Data.SqlClient;
using TecnoMant.Api.Modelos;

namespace TecnoMant.Api.Reglas
{
    public class TecnicoReglas
    {
        private readonly SqlConnection _db;

        public TecnicoReglas(SqlConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Tecnico>> ObtenerTodosAsync()
        {
            var sql = "SELECT IdTecnico, Nombre, DNI AS Dni, Especialidad, Usuario FROM Tecnico ORDER BY Nombre";
            return await _db.QueryAsync<Tecnico>(sql);
        }

        public async Task<Tecnico?> ObtenerPorIdAsync(int id)
        {
            var sql = "SELECT IdTecnico, Nombre, DNI AS Dni, Especialidad, Usuario FROM Tecnico WHERE IdTecnico = @Id";
            return await _db.QueryFirstOrDefaultAsync<Tecnico>(sql, new { Id = id });
        }

        public async Task<int> CrearAsync(DatosTecnico datos)
        {
            datos.Clave = ClaveSegura.Crear(datos.Clave);
            var sql = @"INSERT INTO Tecnico (Nombre, DNI, Especialidad, Usuario, Clave)
                        VALUES (@Nombre, @Dni, @Especialidad, @Usuario, @Clave);
                        SELECT CAST(SCOPE_IDENTITY() as int);";
            return await _db.ExecuteScalarAsync<int>(sql, datos);
        }

        public async Task<bool> ActualizarAsync(int id, DatosTecnico datos)
        {
            if (!string.IsNullOrEmpty(datos.Clave)) datos.Clave = ClaveSegura.Crear(datos.Clave);
            var sql = @"UPDATE Tecnico
                        SET Nombre = @Nombre, DNI = @Dni, Especialidad = @Especialidad, Usuario = @Usuario, Clave = COALESCE(NULLIF(@Clave, ''), Clave)
                        WHERE IdTecnico = @Id";
            var filas = await _db.ExecuteAsync(sql, new { Id = id, datos.Nombre, datos.Dni, datos.Especialidad, datos.Usuario, datos.Clave });
            return filas > 0;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var sql = "DELETE FROM Tecnico WHERE IdTecnico = @Id";
            var filas = await _db.ExecuteAsync(sql, new { Id = id });
            return filas > 0;
        }
    }
}
