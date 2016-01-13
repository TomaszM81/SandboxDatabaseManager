CREATE PROCEDURE [dbo].[CreateDatabaseSnapshot]
(
	@SourceDatabaseName VARCHAR(128),
	@DatabaseSnapshotName VARCHAR(128),
	@DatabaseOwner NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
BEGIN TRY

	IF EXISTS (SELECT 1 FROM dbo.RestrictedDatabases WHERE DatabaseName = @SourceDatabaseName)
		RAISERROR('You are restricted from accessing this database.',11,0);
	
	IF EXISTS (SELECT 1 FROM dbo.RestrictedDatabases WHERE DatabaseName = @DatabaseSnapshotName)
		RAISERROR('You are restricted from using this snapshot name.',11,0);

	IF NOT EXISTS (SELECT 1 FROM sys.databases AS d WHERE d.name = @SourceDatabaseName and d.source_database_id IS NULL)
		RAISERROR('Source database [%s] does not exist.',11,0,@SourceDatabaseName);

	IF EXISTS 
	(
		SELECT 1
		FROM       sys.databases AS d 
		LEFT JOIN dbo.Databases AS d1 ON d.name = d1.DatabaseName
		WHERE d.name = @SourceDatabaseName AND (d1.DatabaseOwner <> @DatabaseOwner OR d1.DatabaseOwner IS NULL)

	) -- someone else's database
	BEGIN
		RAISERROR('You are not allowed to create snapshots on someone else''s database.',11,0);
	END

	IF EXISTS 
	(
		SELECT 1
		FROM       sys.databases AS d 
		LEFT JOIN sys.databases d1 ON d.source_database_id = d1.database_id
		LEFT JOIN dbo.Databases AS d2 ON d1.name = d2.DatabaseName
		WHERE d.name = @DatabaseSnapshotName AND (d.source_database_id IS NULL OR (d.source_database_id IS NOT NULL AND (d2.DatabaseOwner <> @DatabaseOwner OR d2.DatabaseOwner IS NULL)))

	) -- someone else's database 2
	BEGIN
		RAISERROR('You are not allowed to create snapshot with this name.',11,0);
	END


    IF EXISTS 
    (
	    -- Detect if the latest restore operation date (msdb.dbo.restorehistory) is not equal to that stored in dbo.Databases table.
	    -- If it is not then someone has restored onto this database from outside the Sandbox Database Manager tool, we should therefore leave this databae alone and cleanup entry in dbo.Databases table
	    SELECT 1
	    FROM       sys.databases AS d 
	    INNER JOIN dbo.Databases AS d1 ON d.name = d1.DatabaseName
	    CROSS APPLY (select TOP 1 restore_date from msdb.dbo.restorehistory WHERE destination_database_name = @SourceDatabaseName AND restore_type = 'D' ORDER BY restore_history_id desc) LatestRestore(RestoreDate)
	    WHERE d.name = @SourceDatabaseName AND d1.DatabaseOwner = @DatabaseOwner and d1.RestoredOn <> LatestRestore.RestoreDate
    ) 
    BEGIN
	    DELETE FROM dbo.Databases WHERE DatabaseName = @SourceDatabaseName;
	    RAISERROR('Someone has restored onto your database from outside the Sandbox Database Manager tool, the [%s] database is therefore no longer yours.',11,0,@SourceDatabaseName);
    END

    EXEC dbo.DropDatabaseSnapshot @DatabaseSnapshotName, @DatabaseOwner
	
	DECLARE @RestorePathData NVARCHAR(MAX)

	SET @RestorePathData = CAST(SERVERPROPERTY('instancedefaultdatapath') AS NVARCHAR(MAX))

	SELECT TOP 1 @RestorePathData = RTRIM(LTRIM(COALESCE(RestorePathData,CAST(SERVERPROPERTY('instancedefaultdatapath') AS NVARCHAR(MAX))))) 
	FROM dbo.Configuration;
	
	IF COALESCE(@RestorePathData,'') = ''
		RAISERROR('The ''RestorePathData'' is not configured, please insert/update dbo.Configuration.RestorePathData column value.',11,0);
	
	
	RAISERROR('Creating database snapshot [%s].', 0, 0, @DatabaseSnapshotName) with nowait;
	
	DECLARE @SnapshotFiles  NVARCHAR(MAX)	
	DECLARE @SqlStatement NVARCHAR(MAX)
	

    SET @SqlStatement = 'SELECT @SnapshotFiles = COALESCE(@SnapshotFiles,'''') + '',(NAME = ['' + name + ''], FILENAME = ''''' + @RestorePathData + @DatabaseSnapshotName + '_'' + name + ''.ss'''')'' FROM [' + @SourceDatabaseName + '].sys.database_files WHERE type = 0'
    EXEC sp_executesql	@SqlStatement, N'@SnapshotFiles NVARCHAR(MAX) OUT', @SnapshotFiles = @SnapshotFiles OUT;

    SET @SnapshotFiles = STUFF(@SnapshotFiles, 1, 1, '')
    SET @SqlStatement = 'CREATE DATABASE ' + quotename(@DatabaseSnapshotName) + ' ON ' + @SnapshotFiles + ' AS SNAPSHOT OF ' + quotename(@SourceDatabaseName)

    EXEC (@SqlStatement)

    RAISERROR('%s snapshot created.', 0, 0, @DatabaseSnapshotName);
	

END TRY
BEGIN CATCH

	DECLARE 
		@ErrorMessage    NVARCHAR(4000),
		@ErrorNumber     INT,
		@ErrorSeverity   INT,
		@ErrorState      INT,
		@ErrorLine       INT,
		@ErrorProcedure  NVARCHAR(200);

	IF XACT_STATE() <> 0
	BEGIN
		ROLLBACK TRANSACTION;
	END

	IF ERROR_NUMBER() IS NULL
		RETURN;

	SELECT 
		@ErrorNumber = ERROR_NUMBER(),
		@ErrorSeverity = ERROR_SEVERITY(),
		@ErrorState = ERROR_STATE(),
		@ErrorLine = ERROR_LINE(),
		@ErrorMessage = ERROR_MESSAGE(),
		@ErrorProcedure = ISNULL(ERROR_PROCEDURE(), '-');

	RAISERROR(@ErrorMessage, @ErrorSeverity, 1, @ErrorNumber, @ErrorSeverity, @ErrorState, @ErrorProcedure, @ErrorLine);
	
END CATCH;
