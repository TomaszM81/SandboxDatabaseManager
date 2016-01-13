CREATE PROCEDURE dbo.BackupDatabase
(
	@DatabaseName NVARCHAR(128),
	@DatabaseOwner NVARCHAR(128),
	@BackupComment  VARCHAR(200) = null,
	-----------------------
	@TargetBackupFilePath  NVARCHAR(MAX) = null,
	-----------------------
	@TargetBackupDestinationPath NVARCHAR(MAX) = null,
	@Guid NVARCHAR(36) = null,
	@BackupFileName  NVARCHAR(MAX) OUTPUT
	-----------------------
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

IF EXISTS (SELECT 1 FROM dbo.RestrictedDatabases WHERE DatabaseName = @DatabaseName)
BEGIN
		RAISERROR('You are not allowed to backup this database: %s.',11,0, @DatabaseName);
		RETURN 0;
END


IF @TargetBackupFilePath IS NOT NULL
   AND 
   (	@TargetBackupDestinationPath IS NOT NULL
		OR @Guid IS NOT NULL
   )
BEGIN
	RAISERROR('Invalid parameter usage, please supply either @TargetBackupFilePath or (@TargetBackupDestinationPath and @Guid).',11,0);
	RETURN 0
END

DECLARE @CommentFinal VARCHAR(255);

IF LEN(@BackupComment) > 0
	SET @CommentFinal = @BackupComment  + CHAR(13) + CHAR(10) + 'Backup by User: ' + @DatabaseOwner;
ELSE
	SET @CommentFinal = 'Backup by User: ' + @DatabaseOwner;

 
IF @TargetBackupFilePath IS NOT NULL
BEGIN
	RAISERROR('Destination backup file: %s', 0, 0, @TargetBackupFilePath) WITH NOWAIT;
	RAISERROR('', 0, 0) WITH NOWAIT;
END
ELSE	
	SET @TargetBackupFilePath = @TargetBackupDestinationPath + @DatabaseName + '_' + CONVERT(VARCHAR(100),GETDATE(),112) + '_' + @Guid + '.bak';



RAISERROR('[%s] database backup starting on [%s] server...', 0, 0, @DatabaseName, @@SERVERNAME) WITH NOWAIT;
BACKUP DATABASE @DatabaseName TO DISK = @TargetBackupFilePath WITH COPY_ONLY, NOFORMAT, NOINIT,  DESCRIPTION = @CommentFinal, SKIP, NOREWIND, NOUNLOAD, COMPRESSION,  STATS = 5;
RAISERROR('[%s] database backup finished.', 0, 0, @DatabaseName) WITH NOWAIT;

SET @BackupFileName = @TargetBackupFilePath;

