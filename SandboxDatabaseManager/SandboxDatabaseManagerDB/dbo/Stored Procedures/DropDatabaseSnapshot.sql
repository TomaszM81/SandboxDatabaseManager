CREATE PROCEDURE dbo.DropDatabaseSnapshot
(
	 @DatabaseSnapshotName NVARCHAR(128),
	 @DatabaseOwner NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
BEGIN TRY
	
	IF NOT EXISTS (SELECT 1 FROM sys.databases AS d WHERE d.name = @DatabaseSnapshotName and d.source_database_id IS NOT NULL)
		RETURN 0

	IF EXISTS 
	(
		SELECT 1
		FROM       sys.databases AS d 
		LEFT JOIN dbo.Databases AS d1 ON d.name = d1.DatabaseName
		INNER JOIN sys.databases AS d2 ON d.database_id = d2.source_database_id 
		WHERE d2.name = @DatabaseSnapshotName AND (d1.DatabaseOwner <> @DatabaseOwner OR @DatabaseOwner IS NULL)

	) -- someone else's database
	BEGIN
		RAISERROR('You are not allowed to drop someone else''s database snapshot.',11,0);
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

	DECLARE @Statement NVARCHAR(MAX) = 'DROP DATABASE ' + QUOTENAME(@DatabaseSnapshotName)
	EXEC dbo.KillConnections @DatabaseSnapshotName
	EXEC(@Statement);
	RAISERROR('',0,0) WITH NOWAIT;	
	RAISERROR('%s snapshot removed .', 10, 1, @DatabaseSnapshotName) WITH NOWAIT;	

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


