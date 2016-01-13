CREATE PROCEDURE dbo.RestoreDatabase
(
	 @DatabaseName NVARCHAR(128),
	 @FileCollection XML,
	 @PositinInFileCollection INT = 1,
	 @BackupTypeDescription NVARCHAR(128),
	 @DatabaseOwner NVARCHAR(128),
	 @AdHocBackupRestore BIT,
	 @OriginatingServer NVARCHAR(128),
	 @OriginatingDatabaseName NVARCHAR(128),
	 @DatabaseComment NVARCHAR(MAX),
	 @ChangeRecoveryToSimple BIT,
	 @RestoreWithRecovery BIT	 
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
	RAISERROR('You are not allowed to restore using this database name.',11,0);
	RETURN 0
END



DECLARE @Disk NVARCHAR(MAX) = ''

SELECT @Disk += ',DISK = ''' + Files.FilePath.value('.', 'nvarchar(MAX)')  + '''' 
FROM   @FileCollection.nodes('/FileList/FilePath') Files(FilePath)

SET @Disk = STUFF(@Disk,1,1,'')


DECLARE @RecoveryOptionString VARCHAR(50)

If @RestoreWithRecovery = 1
   SET @RecoveryOptionString = 'RECOVERY'
ELSE
   SET @RecoveryOptionString = 'NORECOVERY'


/***********************************************************************************************************/
/*                                 Restore from Full Database Backup                                       */
/***********************************************************************************************************/
IF @BackupTypeDescription = 'Database'
BEGIN


    DECLARE @RestorePathData NVARCHAR(MAX)
    DECLARE @RestorePathLog NVARCHAR(MAX)

    SET @RestorePathData = CAST(SERVERPROPERTY('instancedefaultdatapath') AS NVARCHAR(MAX))
    SET @RestorePathLog = CAST(SERVERPROPERTY('instancedefaultlogpath') AS NVARCHAR(MAX))

    SELECT TOP 1 @RestorePathData = RTRIM(LTRIM(COALESCE(RestorePathData,CAST(SERVERPROPERTY('instancedefaultdatapath') AS NVARCHAR(MAX))))), 
				@RestorePathLog = RTRIM(LTRIM(COALESCE(RestorePathLog,CAST(SERVERPROPERTY('instancedefaultlogpath') AS NVARCHAR(MAX)))))
    FROM dbo.Configuration;
	
    IF COALESCE(@RestorePathData,'') = ''
	    RAISERROR('The ''RestorePathData'' is not configured, please insert/update dbo.Configuration.RestorePathData column value.',11,0);

    IF COALESCE(@RestorePathLog,'') = ''
	    RAISERROR('The ''RestorePathLog'' is not configured, please insert/update dbo.Configuration.RestorePathLog column value.',11,0);

    IF RIGHT(@RestorePathData, 1) <> '\'
	   SET @RestorePathData = @RestorePathData + '\'

    IF RIGHT(@RestorePathLog, 1) <> '\'
	   SET @RestorePathLog = @RestorePathLog + '\'

    IF EXISTS 
    (
	    -- Detect if the latest restore operation date (msdb.dbo.restorehistory) is not equal to that stored in dbo.Databases table.
	    -- If it is not then someone has restored onto this database from outside the Sandbox Database Manager tool, we should therefore leave this database alone and cleanup entry in dbo.Databases table
	    SELECT 1
	    FROM       sys.databases AS d 
	    INNER JOIN dbo.Databases AS d1 ON d.name = d1.DatabaseName
	    CROSS APPLY (select TOP 1 restore_date from msdb.dbo.restorehistory WHERE destination_database_name = @DatabaseName AND restore_type = 'D' ORDER BY restore_history_id desc) LatestRestore(RestoreDate)
	    WHERE d.name = @DatabaseName AND d1.DatabaseOwner = @DatabaseOwner and d1.RestoredOn <> LatestRestore.RestoreDate
    ) 
    BEGIN
	    DELETE FROM dbo.Databases WHERE DatabaseName = @DatabaseName;
	    RAISERROR('Someone has restored onto your database from outside the Sandbox Database Manager tool, the [%s] database is therefore no longer yours.',11,0,@DatabaseName);
	    RETURN 0
    END

    /*************************************    Drop existing database snapshots      ********************************************************/
    if @BackupTypeDescription = 'DATABASE' COLLATE Latin1_General_CI_AS
    BEGIN
	    DECLARE @SnapshotToDrop NVARCHAR(128)

	    DECLARE curDropSnapshots CURSOR FAST_FORWARD FOR
	    SELECT name FROM sys.databases WHERE source_database_id = DB_ID(@DatabaseName);

	    OPEN curDropSnapshots

	    FETCH NEXT FROM curDropSnapshots INTO @SnapshotToDrop

	    WHILE @@FETCH_STATUS = 0
	    BEGIN
		    RAISERROR('Droping database snapshot [%s].', 10, 1, @SnapshotToDrop) WITH NOWAIT;	
		    EXEC dbo.KillConnections @SnapshotToDrop
		    IF(@@error <> 0)
			    RETURN 0;

		    SET @Statement = 'DROP DATABASE ' + QUOTENAME(@SnapshotToDrop);
		    EXEC(@Statement);
		    IF(@@error <> 0)
			    RETURN 0;

		    FETCH NEXT FROM curDropSnapshots INTO @SnapshotToDrop
	    END

	    CLOSE curDropSnapshots
	    DEALLOCATE curDropSnapshots
    END

    /*************************************    Restore the database      ********************************************************/


    IF OBJECT_ID('tempdb..#FileList') IS NOT NULL
	    DROP TABLE #FileList;

    CREATE TABLE #FileList
    (
		LogicalName nvarchar(128) NOT NULL,
		PhysicalName nvarchar(260) NOT NULL,
		Type char(1) NOT NULL,
		FileGroupName nvarchar(120) NULL,
		Size numeric(20, 0) NOT NULL,
		MaxSize numeric(20, 0) NOT NULL,
		FileID bigint NULL,
		CreateLSN numeric(25,0) NULL,
		DropLSN numeric(25,0) NULL,
		UniqueID uniqueidentifier NULL,
		ReadOnlyLSN numeric(25,0) NULL ,
		ReadWriteLSN numeric(25,0) NULL,
		BackupSizeInBytes bigint NULL,
		SourceBlockSize int NULL,
		FileGroupID int NULL,
		LogGroupGUID uniqueidentifier NULL,
		DifferentialBaseLSN numeric(25,0)NULL,
		DifferentialBaseGUID uniqueidentifier NULL,
		IsReadOnly bit NULL,
		IsPresent bit NULL,
		TDEThumbprint varbinary(32) NULL,
		 SnapshotURL nvarchar(360) NULL
	);
 
    DECLARE @ColumnList AS NVARCHAR(MAX) = 'LogicalName,PhysicalName,Type,FileGroupName,Size,MaxSize,FileID,CreateLSN,DropLSN,UniqueID,ReadOnlyLSN,ReadWriteLSN,BackupSizeInBytes,SourceBlockSize,FileGroupID,LogGroupGUID,DifferentialBaseLSN,DifferentialBaseGUID,IsReadOnly,IsPresent,TDEThumbprint,SnapshotURL'

    BEGIN TRY
	   SET @Statement = 'INSERT INTO #FileList(' + @ColumnList + ') ' + CHAR(13) + CHAR(10) + 'EXEC(''RESTORE FILELISTONLY FROM ' + REPLACE(@Disk,'''','''''') + ' WITH FILE = ' + CAST(1 as NVARCHAR(10)) + ''')';
	   EXEC(@Statement)
    END TRY
    BEGIN CATCH
	   BEGIN TRY
	   SET @ColumnList = 'LogicalName,PhysicalName,Type,FileGroupName,Size,MaxSize,FileID,CreateLSN,DropLSN,UniqueID,ReadOnlyLSN,ReadWriteLSN,BackupSizeInBytes,SourceBlockSize,FileGroupID,LogGroupGUID,DifferentialBaseLSN,DifferentialBaseGUID,IsReadOnly,IsPresent,TDEThumbprint'
	   SET @Statement = 'INSERT INTO #FileList(' + @ColumnList + ') ' + CHAR(13) + CHAR(10) + 'EXEC(''RESTORE FILELISTONLY FROM ' + REPLACE(@Disk,'''','''''') + ' WITH FILE = ' + CAST(1 as NVARCHAR(10)) + ''')';
	   EXEC(@Statement)
	   END TRY
	   BEGIN CATCH
		  RAISERROR('Unable to execute RESTORE FILELISTONLY, result set has unsupported format.',11,0);
	   END CATCH
    END CATCH


    DECLARE @GUID VARCHAR(8) = LEFT(CAST(NEWID() AS VARCHAR(36)),8)
    DECLARE @Move NVARCHAR(MAX) = ''

    SELECT  @Move += ', MOVE '''
	    + LogicalName + ''' TO '''
	    + CASE WHEN F.Type = 'D' then @RestorePathData WHEN F.Type = 'L' then @RestorePathLog END 
	    + @DatabaseName 
	    + CASE WHEN F.Type = 'D' then '_Data_' WHEN F.Type = 'L' then '_Log_' END 
	    + CAST(ROW_NUMBER() OVER (PARTITION BY Type ORDER BY (SELECT null))  AS VARCHAR(10))
	    + '_' + @GUID
	    + CASE WHEN F.Type = 'D' then '.mdf' WHEN F.Type = 'L' then '.ldf' END + ''''
    from #FileList F

    SET @Statement =  'RESTORE DATABASE ' + QUOTENAME(@DatabaseName) + ' FROM ' + @Disk + ' WITH FILE = ' + CAST(@PositinInFileCollection AS VARCHAR(10)) + @Move +  ', ' + @RecoveryOptionString + ', REPLACE, STATS = 5'

    RAISERROR('Restoring [%s] database on server %s ...',0,0,@DatabaseName, @@SERVERNAME) WITH NOWAIT

    EXEC dbo.KillConnections @DatabaseName
    EXEC(@Statement)

    IF(@@error <> 0)
    BEGIN	
	    IF NOT EXISTS(SELECT * FROM sys.databases where name = @DatabaseName)
	    BEGIN
		    RAISERROR('Restore of [%s] database has failed.',11,0,@DatabaseName) WITH NOWAIT
		    RETURN 0
	    END
	
	    IF EXISTS(SELECT * FROM sys.databases where name = @DatabaseName AND state <> 0)
	    BEGIN
		    DELETE FROM dbo.Databases WHERE DatabaseName = @DatabaseName;

		    -- attempt to cleanup the mess
		    SET @Statement = 'DROP DATABASE ' + QUOTENAME(@DatabaseName)
		    EXEC(@Statement)

		    RETURN 0
	    END
    END
    ELSE
    BEGIN
	    RAISERROR('',0,0) WITH NOWAIT
	    RAISERROR('Restore of [%s] database completed successfully.',0,0,@DatabaseName) WITH NOWAIT
    END

    DELETE FROM dbo.Databases WHERE DatabaseName = @DatabaseName;

    INSERT INTO dbo.Databases (DatabaseName, Comment, DatabaseOwner, OriginatingServer, OriginatingDatabaseName, SourceBackupFileCollection, RestoredOn)
    VALUES (@DatabaseName, @DatabaseComment, @DatabaseOwner, @OriginatingServer, @OriginatingDatabaseName, CASE WHEN @AdHocBackupRestore = 0 THEN @FileCollection ELSE NULL END, (select TOP 1 restore_date from msdb.dbo.restorehistory WHERE destination_database_name = @DatabaseName ORDER BY restore_history_id desc));


END
/***********************************************************************************************************/
/*                              Restore from Database Differential Backup                                  */
/***********************************************************************************************************/
IF @BackupTypeDescription = 'DATABASE DIFFERENTIAL' COLLATE Latin1_General_CI_AS
BEGIN

    --RESTORE DATABASE [ContosoUniversity] FROM  DISK = N'E:\TargetBackupFolder\Contoso1.bak',  DISK = N'E:\TargetBackupFolder\Contoso2.bak' WITH  FILE = 4,  NORECOVERY,  NOUNLOAD,  STATS = 5

    SET @Statement =  'RESTORE DATABASE ' + QUOTENAME(@DatabaseName) + ' FROM ' + @Disk + ' WITH FILE = ' + CAST(@PositinInFileCollection AS VARCHAR(10)) + ', ' + @RecoveryOptionString + ', NOUNLOAD, STATS = 5'

    RAISERROR('Restoring differential backup onto [%s] database on server %s ...',0,0,@DatabaseName, @@SERVERNAME) WITH NOWAIT

    EXEC(@Statement)

    IF(@@error = 0)
    BEGIN
	    RAISERROR('',0,0) WITH NOWAIT
	    RAISERROR('Restore of differential backup onto [%s] database completed successfully.',0,0,@DatabaseName) WITH NOWAIT
    END

END
/***********************************************************************************************************/
/*                                  Restore from Transaction Log Backup                                    */
/***********************************************************************************************************/
IF @BackupTypeDescription = 'TRANSACTION LOG' COLLATE Latin1_General_CI_AS
BEGIN

SET @Statement = ''
    
    --RESTORE LOG [ContosoUniversity] FROM  DISK = N'E:\TargetBackupFolder\Contoso1.bak',  DISK = N'E:\TargetBackupFolder\Contoso2.bak' WITH  FILE = 5,  NORECOVERY,  NOUNLOAD,  STATS = 5

    SET @Statement =  'RESTORE LOG ' + QUOTENAME(@DatabaseName) + ' FROM ' + @Disk + ' WITH FILE = ' + CAST(@PositinInFileCollection AS VARCHAR(10)) + ', ' + @RecoveryOptionString + ', NOUNLOAD, STATS = 5'

    RAISERROR('Restoring transaction log backup onto [%s] database on server %s ...',0,0,@DatabaseName, @@SERVERNAME) WITH NOWAIT

    EXEC(@Statement)

    IF(@@error = 0)
    BEGIN
	    RAISERROR('',0,0) WITH NOWAIT
	    RAISERROR('Restore of transaction log backup onto [%s] database completed successfully.',0,0,@DatabaseName) WITH NOWAIT
    END
END
/*************************************    Fix recovery to Simple and change owner to sa ********************************************************/

RAISERROR('',0,0) WITH NOWAIT
if @RestoreWithRecovery = 1
BEGIN

SET @Statement =  'ALTER AUTHORIZATION on DATABASE::' + QUOTENAME(@DatabaseName) + ' TO [sa]; ' + CHAR(13) + CHAR(10)

if @ChangeRecoveryToSimple = 1
BEGIN
    RAISERROR('Changing recovery to SIMPLE.',0,0) WITH NOWAIT
    SET @Statement = @Statement + 'ALTER DATABASE ' + QUOTENAME(@DatabaseName) + ' SET RECOVERY SIMPLE; '				
END

--PRINT @Statement
EXEC(@Statement)

END
/*************************************    Shrink log files      ********************************************************/

if @ChangeRecoveryToSimple = 1 AND @RestoreWithRecovery = 1
BEGIN

    RAISERROR('',0,0) WITH NOWAIT
    RAISERROR('Shrinking log files.',0,0) WITH NOWAIT

    SET @Statement = 'USE ' + QUOTENAME(@DatabaseName)  + CHAR(13) + CHAR(10) 
    SELECT @Statement += 'DBCC SHRINKFILE (''' + name + ''' , 0, TRUNCATEONLY) WITH NO_INFOMSGS; ' + CHAR(13) + CHAR(10)  FROM master.sys.master_files WHERE database_id = db_id(@DatabaseName) AND type = 1;

    EXEC(@Statement)
END

/*************************************    Adding User      ********************************************************/

if @RestoreWithRecovery = 1
BEGIN
	EXEC [dbo].[AddUserToDatabase] @DatabaseName, @DatabaseOwner
END

/*************************************    All done      ********************************************************/


If @RestoreWithRecovery = 1
BEGIN
     RAISERROR('',0,0) WITH NOWAIT
	RAISERROR('Database ready for usage.',0,0) WITH NOWAIT
END