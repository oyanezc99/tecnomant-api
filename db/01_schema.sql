/* TecnoMant - Base de datos (SQL Server 2022)
   Modelo físico basado en el Documento 2 (Alcance Técnico). */
CREATE DATABASE TecnoMantDB;
GO
USE TecnoMantDB;
GO

CREATE TABLE Cliente (
    IdCliente   INT IDENTITY(1,1) PRIMARY KEY,
    RazonSocial NVARCHAR(150) NOT NULL,
    Direccion   NVARCHAR(250) NOT NULL,
    Contacto    NVARCHAR(150)
);

CREATE TABLE Equipo (
    IdEquipo    INT IDENTITY(1,1) PRIMARY KEY,
    IdCliente   INT NOT NULL REFERENCES Cliente(IdCliente),
    Tipo        NVARCHAR(30) NOT NULL,      -- Ascensor, Aire acondicionado, Activo industrial
    CodigoSerie NVARCHAR(60) NOT NULL,
    Ubicacion   NVARCHAR(200)
);

CREATE TABLE Tecnico (
    IdTecnico    INT IDENTITY(1,1) PRIMARY KEY,
    Nombre       NVARCHAR(120) NOT NULL,
    DNI          CHAR(8) NOT NULL,
    Especialidad NVARCHAR(60),
    Usuario      NVARCHAR(50) NOT NULL,
    Clave        NVARCHAR(256) NOT NULL       -- hash PBKDF2; las claves antiguas se migran al iniciar sesión
);

CREATE TABLE OrdenTrabajo (
    IdOrden         INT IDENTITY(1,1) PRIMARY KEY,
    IdEquipo        INT NOT NULL REFERENCES Equipo(IdEquipo),
    IdTecnico       INT NOT NULL REFERENCES Tecnico(IdTecnico),
    FechaProgramada DATETIME NOT NULL,
    Estado          NVARCHAR(15) NOT NULL DEFAULT 'Pendiente'   -- Pendiente, En proceso, Completada
);

CREATE TABLE Inspeccion (
    IdInspeccion  INT IDENTITY(1,1) PRIMARY KEY,
    IdOrden       INT NOT NULL REFERENCES OrdenTrabajo(IdOrden),
    FechaHoraReal DATETIME NOT NULL,
    Observaciones NVARCHAR(1000),
    EstadoGeneral NVARCHAR(10) NOT NULL         -- Bien, Regular, Malo
);

CREATE TABLE ItemChecklist (
    IdItem       INT IDENTITY(1,1) PRIMARY KEY,
    IdInspeccion INT NOT NULL REFERENCES Inspeccion(IdInspeccion),
    NombreItem   NVARCHAR(120) NOT NULL,
    Resultado    NVARCHAR(10) NOT NULL,         -- Bien, Regular, Malo
    Observacion  NVARCHAR(500)
);

CREATE TABLE Evidencia (
    IdEvidencia  INT IDENTITY(1,1) PRIMARY KEY,
    IdInspeccion INT NOT NULL REFERENCES Inspeccion(IdInspeccion),
    RutaFoto     NVARCHAR(300) NOT NULL,
    Fecha        DATETIME NOT NULL DEFAULT GETDATE()
);
GO
