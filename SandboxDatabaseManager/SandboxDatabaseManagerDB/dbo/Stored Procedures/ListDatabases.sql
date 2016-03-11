CREATE PROCEDURE [dbo].[ListDatabases]
(
	@DatabaseNameFilter nvarchar(128) = null,
	@DatabaseOwner nvarchar(128) = NULL,
	@Debug BIT = 0
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON


IF len(@DatabaseOwner) = 0
	set @DatabaseOwner = NULL;

IF len(@DatabaseNameFilter) = 0
	set @DatabaseNameFilter = NULL;
ELSE	 
    set @DatabaseNameFilter = @DatabaseNameFilter + '%';


DELETE D
FROM dbo.Databases d
LEFT JOIN sys.databases sd ON d.DatabaseName = sd.name
CROSS APPLY (select TOP 1 restore_date from msdb.dbo.restorehistory WHERE destination_database_name = d.DatabaseName AND restore_type = 'D' ORDER BY restore_history_id desc) LatestRestore(RestoreDate)
WHERE 
	sd.database_id IS NULL -- missing
	OR d.RestoredOn <> LatestRestore.RestoreDate -- someone has restored this database from outside the Sandbox Database Manager tool



CREATE TABLE #ListOfDatabases(
	ID INT NOT NULL PRIMARY KEY,
	[Database Name] NVARCHAR(128) NOT NULL,
	[Size GB] decimal(10, 3) NULL,
	[Owner] nvarchar(100) NULL,
	[Org Server Name] nvarchar(100) NULL,
	[Org Database Name] nvarchar(100) NULL,
	[Source Backup File Collection] xml NULL,
	[Comment] nvarchar(max) NULL,
	[RecoveryModel] nvarchar(100) NULL,
	[DatabaseState] nvarchar(100) NULL,
	[Connections] int NULL,
	[Created On] datetime NULL
)


INSERT INTO #ListOfDatabases
SELECT
	ROW_NUMBER() OVER(ORDER BY (SELECT 1)) ID,
	d.Name DatabaseName,
	SizeGB,
	d1.DatabaseOwner,
	d1.OriginatingServer,
	d1.OriginatingDatabaseName,
	d1.SourceBackupFileCollection,
	d1.Comment,
	d.recovery_model_desc,
	d.state_desc,
	NumOfConnections,
	CASE WHEN d.create_date > coalesce(RestoreDate,'19000101') then d.create_date else coalesce(RestoreDate,'19000101') END CreatedOn	
FROM sys.Databases d
LEFT JOIN dbo.Databases d1
	ON d.name = d1.DatabaseName
LEFT JOIN (SELECT dbid database_id,COUNT(1) NumOfConnections FROM sys.sysprocesses AS s WHERE s.spid > 50 GROUP BY s.dbid) SessionInfo
	ON d.database_id = SessionInfo.database_id
INNER JOIN (SELECT database_id, CAST(((SUM(size) * 8)/1024.0/1024.0) AS DECIMAL(10,3)) SizeGB FROM sys.master_files GROUP by database_id) SizeInfo
	ON d.database_id = SizeInfo.database_id
OUTER APPLY (select TOP 1 restore_date from msdb.dbo.restorehistory WHERE destination_database_name = d.name AND restore_type = 'D' ORDER BY restore_history_id desc) LatestRestore(RestoreDate)
WHERE NOT EXISTS (SELECT 1 FROM dbo.RestrictedDatabases AS rd WHERE rd.DatabaseName = d.name) 
	AND d.database_id > 4
	AND d.source_database_id is NULL
	AND d.user_access = 0 -- multi user
	AND d.collation_name is NOT NULL -- database only if the collation_name column is not null
	--AND d.state = 0 --ONLINE
	AND (d.name LIKE @DatabaseNameFilter OR @DatabaseNameFilter is null)
	AND (@DatabaseOwner IS NULL OR d1.DatabaseOwner = @DatabaseOwner)


--UPDATE dbo.Configuration set VersionColumnSchemaTemplate = '[Data_Ver] NVARCHAR(10) NULL, [Schema_Ver] NVARCHAR(10) NULL';
--UPDATE dbo.Configuration set VersionColumnUpdateStatementPart = 'UPDATE #ListOfDatabases SET 
--                                                                 [Data_Ver] = (SELECT TOP 1 data_ver FROM dbo.upgrade_hist WHERE type = 1 ORDER BY id DESC),  
--												     [Schema_Ver] = (SELECT TOP 1 data_ver FROM dbo.upgrade_hist WHERE type = 2 ORDER BY id DESC)';


DECLARE @VersionColumnSchemaTemplate NVARCHAR(MAX) = (SELECT VersionColumnSchemaTemplate FROM dbo.Configuration)
DECLARE @VersionColumnUpdateStatementPart NVARCHAR(MAX) = (SELECT VersionColumnUpdateStatementPart FROM dbo.Configuration)

IF LEN(@VersionColumnSchemaTemplate) > 0
BEGIN
	EXEC('ALTER TABLE #ListOfDatabases ADD ' + @VersionColumnSchemaTemplate );
	 
	DECLARE @CurrentID INT = 0
	DECLARE @DatabaseName NVARCHAR(MAX)
	DECLARE @Statement NVARCHAR(MAX);	
	SET @Statement = 'DECLARE @ErrorMessage    NVARCHAR(4000);' + CHAR(13) + CHAR(10)


	WHILE(1=1)
	BEGIN
		SELECT TOP 1 @DatabaseName = [Database Name], @CurrentID = ID FROM #ListOfDatabases where DatabaseState = 'ONLINE' AND ID > @CurrentID ORDER BY ID ASC

		IF @@rowcount = 0
			BREAK

	     IF @Debug = 0
		  SET @Statement += 'USE ' + QUOTENAME(@DatabaseName) + '; BEGIN TRY EXEC(''' + @VersionColumnUpdateStatementPart + ' WHERE ID = ' + CAST(@CurrentID AS VARCHAR(10)) + ''') END TRY BEGIN CATCH END CATCH; ' + CHAR(13) + CHAR(10)
	     ELSE
	       SET @Statement += 'USE ' + QUOTENAME(@DatabaseName) + '; BEGIN TRY EXEC(''' + @VersionColumnUpdateStatementPart + ' WHERE ID = ' + CAST(@CurrentID AS VARCHAR(10)) + ''') END TRY BEGIN CATCH SELECT @ErrorMessage = ERROR_MESSAGE(); RAISERROR(''USE ' + QUOTENAME(@DatabaseName) + '; ' + @VersionColumnUpdateStatementPart + ' WHERE ID = ' + CAST(@CurrentID AS VARCHAR(10)) + ''', 0, 1); RAISERROR(@ErrorMessage, 16, 1); END CATCH; ' + CHAR(13) + CHAR(10)

	END
	EXEC (@Statement);

END


SELECT * FROM #ListOfDatabases ORDER by [Database Name];

IF @DatabaseOwner IS NOT NULL
BEGIN

SELECT d.name [Database Snapshot Name], [Database Name], d.create_date [Created On]  
FROM sys.databases AS d
INNER JOIN sys.databases d1 ON d.source_database_id = d1.database_id
INNER JOIN #ListOfDatabases d2 ON d2.[Database Name] = d1.name
WHERE d2.[Owner] = @DatabaseOwner 
ORDER by d.create_date DESC, [Database Name];


END
