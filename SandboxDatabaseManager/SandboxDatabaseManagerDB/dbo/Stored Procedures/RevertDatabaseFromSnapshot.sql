CREATE PROCEDURE dbo.RevertDatabaseFromSnapshot
(
	 @DatabaseSnapshotName NVARCHAR(128),
	 @DatabaseOwner NVARCHAR(100)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
BEGIN TRY
	
	IF NOT EXISTS (SELECT 1 FROM sys.databases AS d WHERE d.name = @DatabaseSnapshotName and d.source_database_id IS NOT NULL)
		RAISERROR('Database snapshot [%s] does not exist.',11,0,@DatabaseSnapshotName);

	IF EXISTS 
	(
		SELECT 1
		FROM       sys.databases AS d 
		INNER JOIN dbo.Databases AS d1 ON d.name = d1.DatabaseName
		INNER JOIN sys.databases AS d2 ON d.database_id = d2.source_database_id 
		WHERE d2.name = @DatabaseSnapshotName AND d1.DatabaseOwner <> @DatabaseOwner

	) -- someone else's database
	BEGIN
		RAISERROR('You are not allowed to revert someone else''s database.',11,0);
	END

	IF EXISTS 
	(
		-- Detect if the latest restore operation date (msdb.dbo.restorehistory) is not equal to that stored in dbo.Databases table.
		-- If it is not then someone has restored onto this database from outside the Sandbox Database Manager tool, we should therefore leave this database alone and cleanup entry in dbo.Databases table
		SELECT 1
		FROM       sys.databases AS d 
		INNER JOIN dbo.Databases AS d1 ON d.name = d1.DatabaseName
		INNER JOIN sys.databases AS d2 ON d.database_id = d2.source_database_id 
		CROSS APPLY (select TOP 1 restore_date from msdb.dbo.restorehistory WHERE destination_database_name = d.name AND restore_type = 'D' ORDER BY restore_history_id desc) LatestRestore(RestoreDate)
		WHERE d2.name = @DatabaseSnapshotName AND d1.DatabaseOwner = @DatabaseOwner and d1.RestoredOn <> LatestRestore.RestoreDate
	) 
	BEGIN
		DELETE d
		FROM dbo.Databases d
		INNER JOIN sys.databases d1 ON d.DatabaseName = d1.name
		INNER JOIN sys.databases d2 ON d1.database_id = d2.source_database_id
		WHERE d2.name = @DatabaseSnapshotName

		RAISERROR('Someone has restored onto your database from outside the Sandbox Database Manager tool, the [%s] database is therefore no longer yours.',11,0,@DatabaseSnapshotName);
	END

	DECLARE @SnapshotToDrop NVARCHAR(128)
	DECLARE @SqlStatement NVARCHAR(MAX)

	DECLARE curDropSnapshots CURSOR FAST_FORWARD FOR
	SELECT d1.name FROM sys.databases d
	INNER JOIN sys.databases d1 ON d.database_id = d1.source_database_id
	INNER JOIN sys.databases d2 ON d.database_id = d2.source_database_id
	WHERE d2.name = @DatabaseSnapshotName AND d1.name <> @DatabaseSnapshotName
	
	OPEN curDropSnapshots

	FETCH NEXT FROM curDropSnapshots INTO @SnapshotToDrop

	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC dbo.DropDatabaseSnapshot @SnapshotToDrop, @DatabaseOwner;	
		FETCH NEXT FROM curDropSnapshots INTO @SnapshotToDrop
	END

	CLOSE curDropSnapshots
	DEALLOCATE curDropSnapshots

	DECLARE @DatabaseName NVARCHAR(128) = (select DB_NAME(source_database_id) FROM sys.databases WHERE name = @DatabaseSnapshotName)
	SET @SqlStatement = 'RESTORE DATABASE ' + QUOTENAME(@DatabaseName) + ' FROM DATABASE_SNAPSHOT = ' +  QUOTENAME(@DatabaseSnapshotName,'''')

	RAISERROR('Killing connections to [%s] database and [%s] database snapshot.', 0, 0, @DatabaseName, @DatabaseSnapshotName) WITH NOWAIT;
	EXEC dbo.KillConnections @DatabaseSnapshotName;
	EXEC dbo.KillConnections @DatabaseName;
	RAISERROR('Reverting [%s] database from [%s] snapshot...', 0, 0, @DatabaseName, @DatabaseSnapshotName) WITH NOWAIT;	
	EXEC(@SqlStatement);
	RAISERROR('', 0, 0) WITH NOWAIT;	
	RAISERROR('Database [%s] reverted from [%s] snapshot .', 0, 0, @DatabaseName, @DatabaseSnapshotName  ) WITH NOWAIT;
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
