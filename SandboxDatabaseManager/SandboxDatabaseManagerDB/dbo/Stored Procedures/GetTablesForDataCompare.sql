CREATE PROCEDURE [dbo].[GetTablesForDataCompare]
(
	@FirstDatabaseName NVARCHAR(128),
	@SecondDatabaseName NVARCHAR(128),
	@TablesToCompare dbo.StringList READONLY
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

IF EXISTS(SELECT * FROM sys.databases WHERE (name  = @FirstDatabaseName OR name = @SecondDatabaseName) and (database_id <= 4 OR name IN (SELECT DatabaseName FROM dbo.RestrictedDatabases)))
BEGIN
    RAISERROR('You are not allowed to access this database',11,0);
    RETURN 0;
END




DECLARE @LikeCondition NVARCHAR(max) = ''

IF EXISTS(SELECT * FROM @TablesToCompare)
BEGIN
	SELECT @LikeCondition += 'OR st.name like ''%' + item + '%''' FROM @TablesToCompare;
	SET @LikeCondition = ' WHERE ' + STUFF(@LikeCondition,1,2,'');
END

DECLARE @SqlStatement AS NVARCHAR(MAX)
SET @SqlStatement = '

USE #FirstDatabaseName#

SELECT FirstDatabaseName.FullTableNameInner FullTableName
	,CASE 
		WHEN FirstDatabaseName.is_primary_key = 1
			AND SecondDatabaseName.is_primary_key = 1
			THEN 1
		ELSE 0
		END HasPK
	,CASE 
		WHEN FirstDatabaseName.Columns = SecondDatabaseName.Columns
			THEN 1
		ELSE 0
		END HasSameColumns
	,CASE 
		WHEN FirstDatabaseName.max_row_count > SecondDatabaseName.max_row_count
			THEN FirstDatabaseName.max_row_count
		ELSE SecondDatabaseName.max_row_count
		END MaxRowCountFromBoth
FROM (
	SELECT STUFF((
				SELECT '','' + QUOTENAME(NAME) AS [text()]
				FROM sys.columns sc
				WHERE sc.object_id = table_object_id
				FOR XML path('''')
				), 1, 1, '''') Columns
		,FullTableNameInner
		,is_primary_key
		,max_row_count
	FROM (
		SELECT QUOTENAME(OBJECT_SCHEMA_NAME(st.object_id)) + ''.'' + QUOTENAME(OBJECT_NAME(st.object_id)) FullTableNameInner
			,st.object_id table_object_id
			,MAX(CAST(is_primary_key AS INT)) is_primary_key
			,MAX(row_count) max_row_count
		FROM sys.tables st
		LEFT JOIN sys.indexes si ON si.object_id = st.object_id
		INNER JOIN sys.dm_db_partition_stats ps ON st.object_id = ps.object_id
			AND si.index_id = ps.index_id ' + @LikeCondition + '
		GROUP BY st.object_id
			,QUOTENAME(OBJECT_SCHEMA_NAME(st.object_id)) + ''.'' + QUOTENAME(OBJECT_NAME(st.object_id))
		) DATA_INNER
	) FirstDatabaseName
INNER JOIN (
	SELECT stuff((
				SELECT '','' + QUOTENAME(NAME) AS [text()]
				FROM #SecondDatabaseName#.sys.columns sc
				WHERE sc.object_id = table_object_id
				FOR XML path('''')
				), 1, 1, '''') Columns
		,FullTableNameInner
		,is_primary_key
		,max_row_count
	FROM (
		SELECT QUOTENAME(ss.NAME) + ''.'' + QUOTENAME(st.NAME) FullTableNameInner
			,st.object_id table_object_id
			,MAX(CAST(is_primary_key AS INT)) is_primary_key
			,MAX(row_count) max_row_count
		FROM #SecondDatabaseName#.sys.tables st
		LEFT JOIN #SecondDatabaseName#.sys.indexes si ON si.object_id = st.object_id
		INNER JOIN #SecondDatabaseName#.sys.dm_db_partition_stats ps ON st.object_id = ps.object_id
			AND si.index_id = ps.index_id
		INNER JOIN #SecondDatabaseName#.sys.schemas ss ON ss.schema_id = st.schema_id ' + @LikeCondition + '
		GROUP BY st.object_id
			,QUOTENAME(ss.NAME) + ''.'' + QUOTENAME(st.NAME)
		) DATA_INNER
	) SecondDatabaseName ON FirstDatabaseName.FullTableNameInner = SecondDatabaseName.FullTableNameInner

'

SET @SqlStatement = REPLACE(REPLACE(@SqlStatement,'#SecondDatabaseName#',QUOTENAME(@SecondDatabaseName)),'#FirstDatabaseName#',QUOTENAME(@FirstDatabaseName));
EXEC(@SqlStatement)
