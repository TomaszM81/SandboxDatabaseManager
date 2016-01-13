CREATE PROCEDURE [dbo].[CheckIfTrackingEnabled]
(
	@DatabaseName NVARCHAR(128),
	@DatabaseOwner NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

IF EXISTS (SELECT 1 FROM dbo.RestrictedDatabases WHERE DatabaseName = @DatabaseName
		   UNION ALL
		   SELECT 1 FROM sys.databases WHERE database_id <= 4 AND NAME = @DatabaseName
		   UNION ALL
		   SELECT 1 FROM sys.databases WHERE source_database_id IS NOT NULL AND NAME = @DatabaseName
		   UNION ALL
		   SELECT 1 FROM sys.databases AS d LEFT JOIN dbo.Databases AS d1 ON d.name = d1.DatabaseName WHERE d.name = @DatabaseName AND (d1.DatabaseOwner <> @DatabaseOwner OR d1.DatabaseOwner IS NULL))
BEGIN
	RAISERROR('You are not allowed to alter this database: %s.',11,0, @DatabaseName);
	RETURN 0;
END

SELECT 1 FROM sys.change_tracking_databases WHERE database_id = DB_ID(@DatabaseName);
