CREATE PROCEDURE dbo.KillConnections
(
	@DatabaseName nvarchar(128)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

if(@DatabaseName is null)
	SET @DatabaseName = (SELECT db_name(dbid) FROM master..sysprocesses where spid = @@spid)

DECLARE @SqlStatement nvarchar(max);
DECLARE @killMessage nvarchar(max);

while exists(SELECT 1 FROM master..sysprocesses where dbid = db_id(@DatabaseName) AND spid <> @@spid and spid > 50)
BEGIN
	set @SqlStatement = ''
	set @killMessage = ''
	select 
		@killMessage += REPLACE(REPLACE(REPLACE(REPLACE('Killing session:#SPID#, from hostname:#HOSTNAME#, login:#LOGIN#, ntuser:#NTUSER#','#SPID#', cast(spid as varchar(10))),'#HOSTNAME#',rtrim(hostname)),'#LOGIN#', rtrim(loginame)),'#NTUSER#', COALESCE(rtrim(nt_domain) + '\','') + COALESCE(rtrim(nt_username),'')) + CHAR(13) + CHAR(10),
		@SqlStatement += REPLACE('BEGIN TRY KILL #SPID#; END TRY BEGIN CATCH PRINT ERROR_MESSAGE() END CATCH;','#SPID#', cast(spid as varchar(10)))
	from master..sysprocesses where dbid = db_id(@DatabaseName) AND spid <> @@spid and spid > 50

	RAISERROR(@killMessage,0,0) WITH NOWAIT;
	exec(@SqlStatement);
	WAITFOR DELAY '00:00:01'						
END
