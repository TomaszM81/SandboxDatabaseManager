CREATE PROCEDURE [dbo].[ValidateRestoreOperation]
(
	@DatabaseName nvarchar(128),
	@DatabaseOwner nvarchar(128),
	@BackupTypeDescription NVARCHAR(128),
	@FirstLSN NUMERIC(25,0) = NULL,
	@LastLSN NUMERIC(25,0) = NULL,
	@DatabaseBackupLSN NUMERIC(25,0) = NULL,
	@CustomErrorMessage VARCHAR(1024) output,
	@CanOverwrite bit output
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

SET @CanOverwrite = NULL;

 -- @CanOverwrite = NULL -> the selected database name is not used, no issues just go
 -- @CanOverwrite = 1    -> this is your database and you can overwrite it
 -- @CanOverwrite = 0    -> this is not your database and you cannot overwrite it


IF EXISTS (SELECT 1 FROM dbo.RestrictedDatabases WHERE DatabaseName = @DatabaseName
		 UNION ALL
		 SELECT 1 FROM sys.databases sd INNER JOIN dbo.Databases d on d.DatabaseName = sd.name WHERE d.DatabaseName = @DatabaseName and d.DatabaseOwner <> @DatabaseOwner)
BEGIN
	SET @CanOverwrite = 0;
	RETURN 0;
END


if @BackupTypeDescription = 'DATABASE' COLLATE Latin1_General_CI_AS
BEGIN

	SET @CanOverwrite = (SELECT TOP 1 CASE WHEN @DatabaseOwner = d1.DatabaseOwner THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END CanOverwrite
						   FROM sys.Databases d
						 LEFT JOIN dbo.Databases d1
						   ON d.name = d1.DatabaseName
						 WHERE d.name = @DatabaseName);
END

IF @BackupTypeDescription = 'DATABASE DIFFERENTIAL' COLLATE Latin1_General_CI_AS
BEGIN
	

    DECLARE @CheckpointLSN NUMERIC(25,0)

    IF NOT EXISTS (SELECT * FROM sys.databases where name = @DatabaseName AND state = 1)
    BEGIN
	   SET @CustomErrorMessage = 'Database ' + QUOTENAME(@DatabaseName) + ' is not in restoring state.'
	   RETURN 0;
    END

    SET @CheckpointLSN = (SELECT TOP 1 b.checkpoint_lsn
					 FROM msdb..restorehistory a
					 INNER JOIN msdb..backupset b ON a.backup_set_id = b.backup_set_id
					 WHERE a.restore_type = 'D' AND a.destination_database_name = @DatabaseName
					 ORDER BY restore_date DESC);

    IF @CheckpointLSN IS NOT NULL AND @DatabaseBackupLSN IS NOT NULL AND @CheckpointLSN <> @DatabaseBackupLSN
    BEGIN
	   SET @CustomErrorMessage = 'Database database differential backup DatabaseBackupLSN of ' + CAST(@DatabaseBackupLSN AS VARCHAR(100)) + ' does not match ' + QUOTENAME(@DatabaseName) + ' database at Checkpoint LSN  of: ' + CAST(@CheckpointLSN AS VARCHAR(100)) + '.' + char(13) + char(10)  + 'Please select matching database differential backup.'
	   RETURN 0;
    END



  
END

IF @BackupTypeDescription = 'TRANSACTION LOG' COLLATE Latin1_General_CI_AS
BEGIN

    IF NOT EXISTS (SELECT * FROM sys.databases where name = @DatabaseName AND state = 1)
    BEGIN
	   SET @CustomErrorMessage = 'Database ' + QUOTENAME(@DatabaseName) + ' is not in restoring state.'
	   RETURN 0;
    END

    DECLARE @DBFirstLSN NUMERIC(25,0)
    DECLARE @DBLastLSN NUMERIC(25,0)


    SELECT TOP 1 @DBFirstLSN = b.first_lsn, @DBLastLSN = b.last_lsn 
    FROM msdb..restorehistory a
    INNER JOIN msdb..backupset b ON a.backup_set_id = b.backup_set_id
    WHERE a.destination_database_name = @DatabaseName
    ORDER BY restore_date DESC;
    
    --you will find a log backup whose first_lsn is less than or equal to the last_lsn from the data or differential backup and whose last_lsn is greater than the last_lsn from the data or differential log backup.

      
    IF @FirstLSN > @DBLastLSN
    BEGIN
       SET @CustomErrorMessage = 'Earlier transaction log file backup is required, DB LastLSN: ' + CAST(@DBLastLSN AS VARCHAR(100)) + ', File FirstLSN: ' + CAST(@FirstLSN AS VARCHAR(100)) + '.'
       RETURN 0;
    END

    
    IF (@LastLSN < @DBLastLSN)
    BEGIN
        SET @CustomErrorMessage = 'Later transaction log file backup is required, DB LastLSN: ' + CAST(@DBLastLSN AS VARCHAR(100)) + ', File LastLSN: ' + CAST(@LastLSN AS VARCHAR(100)) + '.'
       RETURN 0;
    END
    
END