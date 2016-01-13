CREATE PROCEDURE [dbo].[ChangeTrackingEnable]
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

	DECLARE @SqlStatement NVARCHAR(MAX);
	
	IF NOT EXISTS(select * from sys.change_tracking_databases ctd INNER JOIN sys.databases sd ON ctd.database_id = sd.database_id WHERE sd.name = @DatabaseName)
	BEGIN
		SET @SqlStatement = 'ALTER DATABASE ' + QUOTENAME(@DatabaseName) + ' SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 7 DAYS, AUTO_CLEANUP = ON)';
		EXEC(@SqlStatement);
	END


	SET @SqlStatement = 'USE ' +  QUOTENAME(@DatabaseName)  + CHAR(13) + CHAR(10) + '
	DECLARE @Tables AS TABLE(ID INT PRIMARY KEY, SqlStatement NVARCHAR(MAX), Name NVARCHAR(300));

	INSERT @Tables
		SELECT
			ROW_NUMBER() OVER (ORDER BY (SELECT 0)),
			''ALTER TABLE '' + QUOTENAME(OBJECT_SCHEMA_NAME(st.object_id)) + ''.'' + QUOTENAME(st.Name) + '' ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON)'',
			QUOTENAME(OBJECT_SCHEMA_NAME(st.object_id)) + ''.'' + QUOTENAME(st.Name)
		FROM sys.tables st
		INNER JOIN sys.indexes si
			ON st.object_id = si.object_id
			AND si.is_primary_key = 1
		LEFT JOIN sys.change_tracking_tables ct
			ON st.object_id = ct.object_id
		WHERE ct.object_id IS NULL

	DECLARE	@SqlStatement NVARCHAR(MAX),
			@currentID INT = 0,
			@totalCount INT = 0,
			@Name sysname;

	SET @totalCount = (SELECT MAX(ID) FROM @Tables)

	SELECT TOP 1
		@currentID = ID,
		@SqlStatement = SqlStatement,
		@Name = Name
	FROM @Tables
	WHERE ID > @currentID
	ORDER BY ID ASC

	WHILE @@rowcount > 0
	BEGIN
		 EXEC(@SqlStatement);
		 RAISERROR(''Enabled change tracking for %s table, %d of %d tables done.'', 10, 1, @Name, @currentID, @totalCount ) WITH NOWAIT;
		 
		 SELECT TOP 1
			@currentID = ID,
			@SqlStatement = SqlStatement,
			@Name = Name
		 FROM @Tables
		 WHERE ID > @currentID
		 ORDER BY ID ASC
	END

	RAISERROR('''', 0, 0) WITH NOWAIT;
	RAISERROR(''Finished.'', 0, 0) WITH NOWAIT'
	
	EXEC(@SqlStatement);


