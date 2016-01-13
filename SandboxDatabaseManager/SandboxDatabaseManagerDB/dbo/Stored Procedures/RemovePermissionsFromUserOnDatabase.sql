CREATE PROCEDURE [dbo].[RemovePermissionsFromUserOnDatabase]
(
	 @DatabaseName NVARCHAR(128),
	 @OldDatabaseOwner NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @Statement NVARCHAR(MAX)

IF EXISTS (SELECT 1 FROM sys.databases AS d WHERE d.name = @DatabaseName and d.database_id <= 4) OR -- system databases
   EXISTS (SELECT 1 FROM dbo.RestrictedDatabases WHERE DatabaseName = @DatabaseName) OR
   EXISTS (SELECT 1 FROM sys.databases AS d WHERE d.name = @DatabaseName and d.source_database_id IS NOT NULL) OR -- snapshots
   EXISTS (SELECT 1 FROM sys.databases AS d LEFT JOIN dbo.Databases AS d1 ON d.name = d1.DatabaseName WHERE d.name = @DatabaseName AND (d1.DatabaseOwner <> @OldDatabaseOwner OR d1.DatabaseOwner IS NULL)) -- someone else's database
BEGIN
	RAISERROR('You are not allowed to access this database.',11,0);
	RETURN 0
END

RAISERROR('',0,0) WITH NOWAIT
RAISERROR('Removing permissions for %s user from %s database.',0,0, @OldDatabaseOwner, @DatabaseName) WITH NOWAIT


IF EXISTS (SELECT * FROM sys.server_principals AS sp WHERE name = @OldDatabaseOwner)
BEGIN

    DECLARE @DatabaseUser NVARCHAR(MAX)
	SET @Statement = 'USE ' + QUOTENAME(@DatabaseName) + '; SET @DatabaseUser =  (SELECT TOP 1 dp.name FROM sys.server_principals sp INNER JOIN sys.database_principals AS dp on sp.sid = dp.sid WHERE sp.type = ''U'' AND sp.name = @DatabaseOwner)'
	EXEC sp_executesql @Statement, N'@DatabaseOwner NVARCHAR(128), @DatabaseUser NVARCHAR(MAX) OUT', @DatabaseUser = @DatabaseUser OUT, @DatabaseOwner = @OldDatabaseOwner

    IF LEN(@DatabaseUser) > 0
    BEGIN
	   SET @Statement =  'USE ' + QUOTENAME(@DatabaseName) + '; ALTER ROLE [db_owner] DROP MEMBER ' + QUOTENAME(@DatabaseUser) + '; REVOKE BACKUP DATABASE FROM ' + QUOTENAME(@DatabaseUser)
	   EXEC(@Statement)
    END
END
