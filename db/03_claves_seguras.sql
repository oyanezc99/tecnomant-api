-- Ejecutar antes de la versión con claves PBKDF2. No borra datos.
USE TecnoMantDB;
GO
ALTER TABLE Tecnico ALTER COLUMN Clave NVARCHAR(256) NOT NULL;
GO
-- Evita usuarios duplicados, incluso ante dos altas simultáneas.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Tecnico_Usuario' AND object_id=OBJECT_ID('Tecnico'))
BEGIN
    IF EXISTS (SELECT Usuario FROM Tecnico GROUP BY Usuario HAVING COUNT(*)>1)
        THROW 50001, 'Corrige usuarios duplicados antes de crear el índice de unicidad', 1;
    CREATE UNIQUE INDEX UX_Tecnico_Usuario ON Tecnico(Usuario);
END;
GO
