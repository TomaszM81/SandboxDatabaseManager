CREATE PROCEDURE [dbo].[ValidateDatabaseSnaphotName]
(
	@DatabaseSnapshotName nvarchar(128),
	@DatabaseOwner nvarchar(128),
	@CanOverwrite bit output
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

SET @CanOverwrite = NULL;

IF EXISTS (SELECT 1 FROM dbo.RestrictedDatabases WHERE DatabaseName = @DatabaseSnapshotName)
BEGIN
	SET @CanOverwrite = 0;
	RETURN 0;
END

SET @CanOverwrite = (SELECT TOP 1
					CASE
						WHEN d.source_database_id IS NULL THEN CAST(0 AS BIT)
						WHEN d.source_database_id IS NOT NULL AND
							@DatabaseOwner = d3.DatabaseOwner THEN CAST(1 AS BIT)
					END
				FROM sys.Databases d
				LEFT JOIN sys.Databases d2
					ON d.source_database_id = d2.database_id
				LEFT JOIN dbo.Databases d3
					ON d2.name = d3.DatabaseName
				WHERE d.name = @DatabaseSnapshotName);
