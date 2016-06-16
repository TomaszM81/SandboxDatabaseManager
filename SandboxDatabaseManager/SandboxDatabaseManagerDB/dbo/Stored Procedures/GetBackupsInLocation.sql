CREATE PROCEDURE [dbo].[GetBackupsInLocation]
(
	@LocationToInvestigate VARCHAR(8000),
	@FileNameFilter varchar(100) = '',
	@IncludeFull BIT = 1,
	@IncludeDiff BIT = 0,
	@IncludeLog BIT = 0,
	@LogMessage varchar(max) output
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @BackupTypes AS TABLE(BackupType SMALLINT PRIMARY KEY)

INSERT INTO @BackupTypes
SELECT 1 WHERE @IncludeFull = 1
UNION ALL 
SELECT 5 WHERE @IncludeDiff = 1
UNION ALL 
SELECT 2 WHERE @IncludeLog = 1

SET @LogMessage = ''

IF RIGHT(@LocationToInvestigate,1) <> '\'
	SET @LocationToInvestigate += '\'

DECLARE @FolderStructure as TABLE(ID INT IDENTITY(1,1), Name NVARCHAR(MAX), Depth INT, IsFile INT)
DECLARE @FolderStructureFinal as TABLE(ID INT, Name NVARCHAR(MAX), Depth INT, IsFile INT, MaxSupportedID INT)

INSERT into @FolderStructure EXEC xp_dirtree @LocationToInvestigate,0,1

INSERT INTO  @FolderStructureFinal
select ID, Name, Depth, IsFile, (SELECT TOP 1 id FROM @FolderStructure DD WHERE DD.ID > D.ID and DD.Depth = D.Depth and IsFile = 0 AND D.IsFile = 0 ORDER by DD.ID) MaxSupportedID FROM @FolderStructure D where Depth = 1


DECLARE @CurrentDepth INT = 0

WHILE (@@rowcount > 0)
BEGIN
	SET @CurrentDepth += 1;

	INSERT INTO  @FolderStructureFinal
	SELECT F.ID, D.Name + '\' + F.Name, F.Depth, F.IsFile, (SELECT TOP 1 id FROM @FolderStructure DD WHERE DD.ID > F.ID and DD.Depth = F.Depth and DD.IsFile = 0 AND F.IsFile = 0 ORDER by DD.ID ASC) MaxSupportedID 
	FROM @FolderStructure F
	INNER JOIN @FolderStructureFinal D 
		ON F.Depth = D.Depth + 1 and D.IsFile = 0 AND F.ID > D.ID AND (MaxSupportedID > F.ID OR MaxSupportedID IS NULL)
	WHERE D.Depth = @CurrentDepth 
END


DECLARE @FilesTable as TABLE(ID INT PRIMARY KEY, FilePath NVARCHAR(MAX))






INSERT INTO @FilesTable
select ROW_NUMBER() OVER (order by (SELECT 1)), @LocationToInvestigate + Name from @FolderStructureFinal WHERE IsFile = 1 and Name LIKE '%.b[ac]k' AND Name like '%' + @FileNameFilter + '%'


/*******************************************************************************************************************************/
/*                               List and detect incomplete and duplicated files                                                */
/*******************************************************************************************************************************/


DECLARE @LabelResult as TABLE(
	 ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	 FilePath NVARCHAR(max) NULL,
	 MediaName   int  NULL,
	 MediaSetId   uniqueidentifier  NULL,
	 FamilyCount   int  NOT NULL,
	 FamilySequenceNumber   int  NOT NULL,
	 MediaFamilyId   uniqueidentifier  NULL,
	 MediaSequenceNumber   int  NOT NULL,
	 MediaLabelPresent   int  NOT NULL,
	 MediaDescription   int  NULL,
	 SoftwareName   varchar (20) NOT NULL,
	 SoftwareVendorId   int  NOT NULL,
	 MediaDate   datetime  NULL,
	 MirrorCount   int  NOT NULL,
	 IsCompressed   int  NOT NULL
)



DECLARE @CurrentID INT = 0
DECLARE @FilePath NVARCHAR(MAX)
WHILE(1=1)
BEGIN
	SELECT TOP 1 @FilePath = FilePath, @CurrentID = ID FROM @FilesTable where ID > @CurrentID ORDER BY ID ASC

	IF @@rowcount = 0
		BREAK

	INSERT INTO @LabelResult(MediaName, MediaSetId, FamilyCount, FamilySequenceNumber, MediaFamilyId, MediaSequenceNumber, MediaLabelPresent, MediaDescription, SoftwareName, SoftwareVendorId, MediaDate, MirrorCount, IsCompressed)
	EXEC('RESTORE LABELONLY FROM DISK = ''' + @FilePath + '''');
	
	UPDATE @LabelResult 
		SET FilePath = @FilePath
	WHERE ID = SCOPE_IDENTITY()

END


/*------------------------------- Duplicates --------------------------------------*/

DECLARE @DuplicateFileMessage NVARCHAR(MAX) = ''

select @DuplicateFileMessage += ' File ' + D1.FilePath + ' is a duplicate of ' + D2.FilePath + ', file will not be used.' + CHAR(13) + CHAR(10) from 
(select FilePath, MediaSetId, MediaFamilyId, ROW_NUMBER() OVER(PARTITION BY MediaSetId, MediaFamilyId ORDER BY LEN(FilePath) ASC) DuplicateNo from @LabelResult) D1
INNER JOIN 
(
	select * from 
	( select FilePath,MediaSetId, MediaFamilyId, ROW_NUMBER() OVER(PARTITION BY MediaSetId, MediaFamilyId ORDER BY LEN(FilePath) ASC) DuplicateNo from @LabelResult) D
	WHERE D.DuplicateNo = 1
) D2 ON D1.MediaSetId = D2.MediaSetId and D1.MediaFamilyId = D2.MediaFamilyId
WHERE D1.DuplicateNo > 1


IF LEN(@DuplicateFileMessage) > 0
	SET @DuplicateFileMessage =  'Following files are duplicates and will not be used:' + CHAR(13) + CHAR(10) + @DuplicateFileMessage 


DELETE FROM @LabelResult
WHERE ID IN 
(
	select ID from 
	( select ID,FilePath,MediaSetId, MediaFamilyId, FamilyCount, FamilySequenceNumber, ROW_NUMBER() OVER(PARTITION BY MediaSetId, MediaFamilyId ORDER BY LEN(FilePath) ASC) DuplicateNo from @LabelResult) D
	WHERE D.DuplicateNo > 1
)

SET @LogMessage += @DuplicateFileMessage;
--PRINT @DuplicateFileMessage

/*------------------------------- Incomplete File Collections --------------------------------------*/

DECLARE @MissingFileMessage NVARCHAR(MAX) = ''

select @MissingFileMessage += 'File collection: (' + STUFF(D1.FileCollection,1,2,'') + ') is missing ' + cast((FilesCountExpected - FilesCountReal) as varchar(10)) + ' file(s), collection will no be used.' + CHAR(13) + CHAR(10)
from 
(SELECT MediaSetId, FamilyCount FilesCountExpected, COUNT(1) FilesCountReal FROM @LabelResult GROUP BY MediaSetId, FamilyCount HAVING COUNT(1) <> FamilyCount) D
CROSS APPLY
(
	SELECT ', ' + FilePath [text()] FROM @LabelResult L WHERE L.MediaSetId = D.MediaSetId FOR XML PATH ('')
) D1(FileCollection)


IF LEN(@MissingFileMessage) > 0
	SET @MissingFileMessage =  'Following file collections are missing some files:' + CHAR(13) + CHAR(10) + @MissingFileMessage


SET @LogMessage += @MissingFileMessage;

DELETE FROM @LabelResult where MediaSetId IN 
(SELECT MediaSetId FilesCountReal FROM @LabelResult GROUP BY MediaSetId, FamilyCount HAVING COUNT(1) <> FamilyCount)


/*******************************************************************************************************************************/
/*                                    Get available databases from backup Files                                                */
/*******************************************************************************************************************************/

DECLARE @BackupFileList as TABLE(ID INT IDENTITY(1,1) PRIMARY KEY, FilePath NVARCHAR(MAX), FileList NVARCHAR(MAX))

INSERT INTO @BackupFileList
SELECT FilePath, (SELECT D.FilePath FROM @LabelResult D WHERE D.MediaSetId = F.MediaSetId FOR XML PATH(''), ROOT('FileList'), ELEMENTS) FileList
FROM @LabelResult F
WHERE FamilySequenceNumber = 1;

IF OBJECT_ID('tempdb..#AvailableDatabases') IS NOT NULL
	DROP TABLE #AvailableDatabases;

CREATE TABLE #AvailableDatabases
(
	FilePath NVARCHAR(MAX) NULL
	,FileList NVARCHAR(MAX) NULL
	----
	,BackupName NVARCHAR(128)
	,BackupDescription NVARCHAR(255)
	,BackupType SMALLINT
	,ExpirationDate DATETIME
	,Compressed BIT
	,Position SMALLINT
	,DeviceType TINYINT
	,UserName NVARCHAR(128)
	,ServerName NVARCHAR(128)
	,DatabaseName NVARCHAR(128)
	,DatabaseVersion INT
	,DatabaseCreationDate DATETIME
	,BackupSize NUMERIC(20, 0)
	,FirstLSN NUMERIC(25, 0)
	,LastLSN NUMERIC(25, 0)
	,CheckpointLSN NUMERIC(25, 0)
	,DatabaseBackupLSN NUMERIC(25, 0)
	,BackupStartDate DATETIME
	,BackupFinishDate DATETIME
	,SortOrder SMALLINT
	,CodePage SMALLINT
	,UnicodeLocaleId INT
	,UnicodeComparisonStyle INT
	,CompatibilityLevel TINYINT
	,SoftwareVendorId INT
	,SoftwareVersionMajor INT
	,SoftwareVersionMinor INT
	,SoftwareVersionBuild INT
	,MachineName NVARCHAR(128)
	,Flags INT
	,BindingID UNIQUEIDENTIFIER
	,RecoveryForkID UNIQUEIDENTIFIER
	,Collation NVARCHAR(128)
	,FamilyGUID UNIQUEIDENTIFIER
	,HasBulkLoggedData BIT
	,IsSnapshot BIT
	,IsReadOnly BIT
	,IsSingleUser BIT
	,HasBackupChecksums BIT
	,IsDamaged BIT
	,BeginsLogChain BIT
	,HasIncompleteMetaData BIT
	,IsForceOffline BIT
	,IsCopyOnly BIT
	,FirstRecoveryForkID UNIQUEIDENTIFIER
	,ForkPointLSN NUMERIC(25,0) NULL
	,RecoveryModel NVARCHAR(60)
	,DifferentialBaseLSN NUMERIC(25,0) NULL
	,DifferentialBaseGUID UNIQUEIDENTIFIER
	,BackupTypeDescription NVARCHAR(60)
	,BackupSetGUID UNIQUEIDENTIFIER NULL
	,CompressedBackupSize BIGINT NULL
	,containment TINYINT NULL
	,KeyAlgorithm NVARCHAR(32) NULL
	,EncryptorThumbprint VARBINARY(20) NULL
	,EncryptorType NVARCHAR(32) NULL
	)


DECLARE @CurrentID_1 INT = 0
DECLARE @FilePath_1 NVARCHAR(MAX)
DECLARE @FileList_1 NVARCHAR(MAX)



DECLARE @ColumnList AS VARCHAR(MAX)


 SET @ColumnList = 'BackupName, BackupDescription, BackupType, ExpirationDate, Compressed, Position, DeviceType, UserName, ServerName, DatabaseName, DatabaseVersion, DatabaseCreationDate, BackupSize, FirstLSN, LastLSN, CheckpointLSN, DatabaseBackupLSN, BackupStartDate, BackupFinishDate, SortOrder, [CodePage], UnicodeLocaleId, UnicodeComparisonStyle, CompatibilityLevel, SoftwareVendorId, SoftwareVersionMajor, SoftwareVersionMinor, SoftwareVersionBuild, MachineName, Flags, BindingID, RecoveryForkID, Collation, FamilyGUID, HasBulkLoggedData, IsSnapshot, IsReadOnly, IsSingleUser, HasBackupChecksums, IsDamaged, BeginsLogChain, HasIncompleteMetaData, IsForceOffline, IsCopyOnly, FirstRecoveryForkID, ForkPointLSN, RecoveryModel, DifferentialBaseLSN, DifferentialBaseGUID, BackupTypeDescription, BackupSetGUID, CompressedBackupSize, containment, KeyAlgorithm, EncryptorThumbprint, EncryptorType'


DECLARE @Statement VARCHAR(MAX) = ''
WHILE(1=1)
BEGIN
	SELECT TOP 1 @FilePath_1 = FilePath, @FileList_1 = FileList, @CurrentID_1 = ID FROM @BackupFileList where ID > @CurrentID_1 ORDER BY ID ASC

	IF @@rowcount = 0
		BREAK

     BEGIN TRY
	    SET @ColumnList = 'BackupName, BackupDescription, BackupType, ExpirationDate, Compressed, Position, DeviceType, UserName, ServerName, DatabaseName, DatabaseVersion, DatabaseCreationDate, BackupSize, FirstLSN, LastLSN, CheckpointLSN, DatabaseBackupLSN, BackupStartDate, BackupFinishDate, SortOrder, [CodePage], UnicodeLocaleId, UnicodeComparisonStyle, CompatibilityLevel, SoftwareVendorId, SoftwareVersionMajor, SoftwareVersionMinor, SoftwareVersionBuild, MachineName, Flags, BindingID, RecoveryForkID, Collation, FamilyGUID, HasBulkLoggedData, IsSnapshot, IsReadOnly, IsSingleUser, HasBackupChecksums, IsDamaged, BeginsLogChain, HasIncompleteMetaData, IsForceOffline, IsCopyOnly, FirstRecoveryForkID, ForkPointLSN, RecoveryModel, DifferentialBaseLSN, DifferentialBaseGUID, BackupTypeDescription, BackupSetGUID, CompressedBackupSize, containment, KeyAlgorithm, EncryptorThumbprint, EncryptorType'
	    SET @Statement = 'INSERT INTO #AvailableDatabases(' + @ColumnList + ') ' + CHAR(13) + CHAR(10) +
	              		'EXEC(''RESTORE HEADERONLY FROM DISK = ''''' + @FilePath_1 + ''''''')';
	    EXEC(@Statement);
     END TRY
	BEGIN CATCH
	   BEGIN TRY
		 SET @ColumnList = 'BackupName, BackupDescription, BackupType, ExpirationDate, Compressed, Position, DeviceType, UserName, ServerName, DatabaseName, DatabaseVersion, DatabaseCreationDate, BackupSize, FirstLSN, LastLSN, CheckpointLSN, DatabaseBackupLSN, BackupStartDate, BackupFinishDate, SortOrder, [CodePage], UnicodeLocaleId, UnicodeComparisonStyle, CompatibilityLevel, SoftwareVendorId, SoftwareVersionMajor, SoftwareVersionMinor, SoftwareVersionBuild, MachineName, Flags, BindingID, RecoveryForkID, Collation, FamilyGUID, HasBulkLoggedData, IsSnapshot, IsReadOnly, IsSingleUser, HasBackupChecksums, IsDamaged, BeginsLogChain, HasIncompleteMetaData, IsForceOffline, IsCopyOnly, FirstRecoveryForkID, ForkPointLSN, RecoveryModel, DifferentialBaseLSN, DifferentialBaseGUID, BackupTypeDescription, BackupSetGUID, CompressedBackupSize, containment'
	      SET @Statement = 'INSERT INTO #AvailableDatabases(' + @ColumnList + ') ' + CHAR(13) + CHAR(10) +
	              		'EXEC(''RESTORE HEADERONLY FROM DISK = ''''' + @FilePath_1 + ''''''')';
	      EXEC(@Statement);
	   END TRY
	   BEGIN CATCH
		  BEGIN TRY
			SET @ColumnList = 'BackupName, BackupDescription, BackupType, ExpirationDate, Compressed, Position, DeviceType, UserName, ServerName, DatabaseName, DatabaseVersion, DatabaseCreationDate, BackupSize, FirstLSN, LastLSN, CheckpointLSN, DatabaseBackupLSN, BackupStartDate, BackupFinishDate, SortOrder, [CodePage], UnicodeLocaleId, UnicodeComparisonStyle, CompatibilityLevel, SoftwareVendorId, SoftwareVersionMajor, SoftwareVersionMinor, SoftwareVersionBuild, MachineName, Flags, BindingID, RecoveryForkID, Collation, FamilyGUID, HasBulkLoggedData, IsSnapshot, IsReadOnly, IsSingleUser, HasBackupChecksums, IsDamaged, BeginsLogChain, HasIncompleteMetaData, IsForceOffline, IsCopyOnly, FirstRecoveryForkID, ForkPointLSN, RecoveryModel, DifferentialBaseLSN, DifferentialBaseGUID, BackupTypeDescription, BackupSetGUID, CompressedBackupSize'
			SET @Statement = 'INSERT INTO #AvailableDatabases(' + @ColumnList + ') ' + CHAR(13) + CHAR(10) +
	              		'EXEC(''RESTORE HEADERONLY FROM DISK = ''''' + @FilePath_1 + ''''''')';
	          EXEC(@Statement);
		  END TRY
		  BEGIN CATCH
			 RAISERROR('Unable to execute RESTORE HEADERONLY, result set has unsupported format.',11,0);
		  END CATCH
	   END CATCH
	END CATCH

	UPDATE #AvailableDatabases 
		SET FilePath = @FilePath_1,
			FileList = @FileList_1
	WHERE FilePath IS NULL;

END


SELECT
	FilePath, -- 2
	FileList, -- 3
	BackupDescription, -- 4
	REVERSE(LEFT(REVERSE(FilePath), CHARINDEX('\', REVERSE(FilePath)) - 1)) [File Name], -- 5
	DatabaseName [Database Name], -- 6
	BackupTypeDescription [Backup Type], --7
	UserName [Backup User Name], -- 8
	ServerName [Backup Server Name], -- 9
	CAST(BackupSize / 1073741824  AS decimal(11, 3)) [Backup Size GB], -- 10
	Position [Position In File], -- 11
	BackupStartDate [Backup Date], -- 12
	DatabaseCreationDate [Org Database Creation Date],	 -- 13
	CASE SoftwareVersionMajor
	   WHEN 9 THEN 
		  CASE 
			 WHEN SoftwareVersionBuild >= 5000 THEN 'SQL Server 2005 SP4'
			 WHEN SoftwareVersionBuild >= 4035 THEN 'SQL Server 2005 SP3'
			 WHEN SoftwareVersionBuild >= 3042 THEN 'SQL Server 2005 SP2'
			 WHEN SoftwareVersionBuild >= 2047 THEN 'SQL Server 2005 SP1'
			 ELSE 'SQL Server 2005'
		  END
	   WHEN 10 THEN  
		  CASE SoftwareVersionMinor
			 WHEN 0 THEN 
				CASE 
				    WHEN SoftwareVersionBuild >= 6000 THEN 'SQL Server 2008 SP4'
				    WHEN SoftwareVersionBuild >= 5500 THEN 'SQL Server 2008 SP3'
				    WHEN SoftwareVersionBuild >= 4000 THEN 'SQL Server 2008 SP2'
				    WHEN SoftwareVersionBuild >= 2531 THEN 'SQL Server 2008 SP1'
				    ELSE 'SQL Server 2008'
				END
			 WHEN 50 THEN
				CASE 
				    WHEN SoftwareVersionBuild >= 6000 THEN 'SQL Server 2008 R2 SP3'
				    WHEN SoftwareVersionBuild >= 4000 THEN 'SQL Server 2008 R2 SP2'
				    WHEN SoftwareVersionBuild >= 2500 THEN 'SQL Server 2008 R2 SP1'
				    ELSE 'SQL Server 2008 R2'
				END
		  END
	   WHEN 11 THEN
			    CASE 
					WHEN SoftwareVersionBuild >= 6020 THEN 'SQL Server 2012 SP3'
				    WHEN SoftwareVersionBuild >= 5058 THEN 'SQL Server 2012 SP2'
				    WHEN SoftwareVersionBuild >= 3000 THEN 'SQL Server 2012 SP1'
				    ELSE 'SQL Server 2012'
				END
	   WHEN 12 THEN
				CASE 
				    WHEN SoftwareVersionBuild >= 4100 THEN 'SQL Server 2014 SP1'
				    ELSE 'SQL Server 2014'
				END
	   ELSE  'SQL Server'
     END [SQL Server Version], -- 14
	CompatibilityLevel [Compatibility Level], -- 15
	RecoveryModel [Recovery Model], -- 16
     FirstLSN [First LSN], -- 17
     LastLSN [Last LSN],-- 18
     CheckpointLSN [Checkpoint LSN], -- 19
     DatabaseBackupLSN [DatabaseBackup LSN] -- 20
FROM #AvailableDatabases -- 16
INNER JOIN @BackupTypes on #AvailableDatabases.BackupType = [@BackupTypes].BackupType
GO
