/* Datos de prueba (ficticios) */
USE TecnoMantDB;
GO

INSERT INTO Cliente (RazonSocial, Direccion, Contacto) VALUES
 (N'Edificio Residencial Los Álamos', N'Av. Arequipa 1234, Lince', N'999000111'),
 (N'Planta Industrial Andina SAC',    N'Av. Argentina 4500, Callao', N'999000222'),
 (N'Condominio Vista Mar',            N'Av. La Marina 2100, San Miguel', N'999000333');

INSERT INTO Equipo (IdCliente, Tipo, CodigoSerie, Ubicacion) VALUES
 (1, N'Ascensor',           N'ASC-001', N'Torre A'),
 (1, N'Ascensor',           N'ASC-002', N'Torre B'),
 (2, N'Activo industrial',  N'IND-010', N'Línea 1'),
 (2, N'Aire acondicionado', N'AC-003',  N'Oficinas'),
 (3, N'Ascensor',           N'ASC-004', N'Sótano 2');

INSERT INTO Tecnico (Nombre, DNI, Especialidad, Usuario, Clave) VALUES
 (N'Tecnico Prueba 1', '10000001', N'Ascensores',         N'tec01', N'1234'),
 (N'Tecnico Prueba 2', '10000002', N'Aire acondicionado', N'tec02', N'1234');

INSERT INTO OrdenTrabajo (IdEquipo, IdTecnico, FechaProgramada, Estado) VALUES
 (1, 1, DATEADD(DAY, 1, GETDATE()), N'Pendiente'),
 (2, 1, DATEADD(DAY, 2, GETDATE()), N'Pendiente'),
 (5, 1, DATEADD(DAY, 3, GETDATE()), N'En proceso'),
 (3, 2, DATEADD(DAY, 1, GETDATE()), N'Pendiente'),
 (4, 2, DATEADD(DAY, 4, GETDATE()), N'Pendiente');
GO
