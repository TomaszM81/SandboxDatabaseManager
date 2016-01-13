CREATE PROCEDURE [dbo].[AddUserToDatabase]
(
	 @DatabaseName NVARCHAR(128),
	 @DatabaseOwner NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @Statement NVARCHAR(MAX)


IF EXISTS (SELECT 1 FROM sys.databases AS d WHERE d.name = @DatabaseName and d.database_id <= 4) OR -- system databases
   EXISTS (SELECT 1 FROM dbo.RestrictedDatabases WHERE DatabaseName = @DatabaseName) OR
   EXISTS (SELECT 1 FROM sys.databases AS d WHERE d.name = @DatabaseName and d.source_database_id IS NOT NULL) OR -- snapshots
   EXISTS (SELECT 1 FROM sys.databases AS d LEFT JOIN dbo.Databases AS d1 ON d.name = d1.DatabaseName WHERE d.name = @DatabaseName AND (d1.DatabaseOwner <> @DatabaseOwner OR d1.DatabaseOwner IS NULL)) -- someone else's database
BEGIN
	RAISERROR('You are not allowed to access this database.',11,0);
	RETURN 0
END

RAISERROR('',0,0) WITH NOWAIT
RAISERROR('Adding %s user to %s database.',0,0, @DatabaseOwner, @DatabaseName) WITH NOWAIT


DECLARE @addRoleSubStatement VARCHAR(100)

SET @addRoleSubStatement = 'ALTER ROLE [db_owner] ADD MEMBER '

DECLARE @Version VARCHAR(50);
SET @Version = CAST(SERVERPROPERTY('ProductVersion') as VARCHAR(50))

if CAST(SUBSTRING(@Version,1,charindex('.',@Version,1) - 1) AS INT) < 11
BEGIN
	SET @addRoleSubStatement = 'EXEC sp_addrolemember [db_owner], '
END


IF EXISTS (SELECT * FROM sys.server_principals AS sp WHERE name = @DatabaseOwner)
BEGIN

    DECLARE @DatabaseUser NVARCHAR(MAX)
    SET @Statement = 'USE ' + QUOTENAME(@DatabaseName) + '; SET @DatabaseUser =  (SELECT TOP 1 dp.name FROM sys.server_principals sp INNER JOIN sys.database_principals AS dp on sp.sid = dp.sid WHERE sp.type = ''U'' AND sp.name = @DatabaseOwner)'
    EXEC sp_executesql @Statement, N'@DatabaseOwner NVARCHAR(128), @DatabaseUser NVARCHAR(MAX) OUT', @DatabaseUser = @DatabaseUser OUT, @DatabaseOwner = @DatabaseOwner

    IF LEN(@DatabaseUser) > 0
    BEGIN
	   SET @Statement =  'USE ' + QUOTENAME(@DatabaseName) + '; ' + @addRoleSubStatement + QUOTENAME(@DatabaseUser) + '; DENY BACKUP DATABASE TO ' + QUOTENAME(@DatabaseUser)
	   EXEC(@Statement)
    END
    ELSE 
    BEGIN

	   SET @Statement = 'USE ' + QUOTENAME(@DatabaseName) + '; SET @DatabaseUser =  (SELECT name FROM sys.database_principals WHERE name = @DatabaseOwner)'
	   EXEC sp_executesql @Statement, N'@DatabaseOwner NVARCHAR(128), @DatabaseUser NVARCHAR(MAX) OUT', @DatabaseUser = @DatabaseUser OUT, @DatabaseOwner = @DatabaseOwner

	  IF LEN(@DatabaseUser) > 0
	   BEGIN
		  SET @Statement =  'USE ' + QUOTENAME(@DatabaseName) + '; ALTER USER ' + QUOTENAME(@DatabaseOwner) + ' WITH LOGIN = ' + QUOTENAME(@DatabaseOwner) + '; ALTER ROLE [db_owner] ADD MEMBER ' + QUOTENAME(@DatabaseOwner) + '; DENY BACKUP DATABASE TO ' + QUOTENAME(@DatabaseOwner)
		  EXEC(@Statement)
	   END
	   ELSE
	   BEGIN
		
		  SET @Statement =  'USE ' + QUOTENAME(@DatabaseName) + '; CREATE USER ' + QUOTENAME(@DatabaseOwner) + ' FROM LOGIN ' + QUOTENAME(@DatabaseOwner) + ' WITH DEFAULT_SCHEMA=[dbo]'
		  EXEC(@Statement)
		  SET @Statement =  'USE ' + QUOTENAME(@DatabaseName) + '; ' + @addRoleSubStatement + QUOTENAME(@DatabaseOwner) + '; DENY BACKUP DATABASE TO ' + QUOTENAME(@DatabaseOwner)
		  EXEC(@Statement)  
	   END
    END
END
GO