CREATE PROCEDURE [dbo].[GetTablesForDataTrack]
(
	@DatabaseName NVARCHAR(100),
	@DatabaseOwner NVARCHAR(100),
	@TableNames dbo.StringList READONLY
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

IF EXISTS(SELECT 1 FROM sys.databases WHERE name  = @DatabaseName and (database_id <= 4 OR name IN (SELECT DatabaseName FROM dbo.RestrictedDatabases))
		  UNION ALL
		  SELECT 1 FROM sys.databases AS d LEFT JOIN dbo.Databases AS d1 ON d.name = d1.DatabaseName WHERE d.name = @DatabaseName AND (d1.DatabaseOwner <> @DatabaseOwner OR d1.DatabaseOwner IS NULL))
BEGIN
    RAISERROR('You are not allowed to access this database',11,0);
    RETURN 0;
END

DECLARE @SqlStatement AS NVARCHAR(MAX)
DECLARE @LikeCondition NVARCHAR(MAX) = ''

IF EXISTS(SELECT * FROM @TableNames)
BEGIN
	SELECT @LikeCondition += 'OR st.name like ''%' + item + '%''' FROM @TableNames
	SET @LikeCondition = ' WHERE ' + stuff(@LikeCondition,1,2,'')
END





SET @SqlStatement = '
USE #DatabaseName#

SELECT ''SELECT '''''' + QUOTENAME(OBJECT_SCHEMA_NAME(st.object_id)) + ''.'' + QUOTENAME(st.NAME) + '''''' TableName, CASE CT.SYS_CHANGE_OPERATION WHEN ''''U'''' THEN ''''Updated'''' WHEN ''''I'''' then ''''Inserted'''' else ''''Deleted'''' END RowChangeType,  '' + '' REPLACE(REPLACE(REVERSE(SUBSTRING(REVERSE(SUBSTRING((select '' + indexColumns + '' for xml raw(''''key'''') , BINARY BASE64), 6,500)),3,500)),''''&lt;'''',''''<''''),''''&gt;'''',''''>'''') TableKeyValues, CT.SYS_CHANGE_VERSION, ''''<table id="track_result">'' + headerColumnsHtml + '''''' +  REPLACE(xmlRowData ,''''TDColor>'''',''''TD class="row_diff">'''') + ''''</table>'''' RowData '' + ''FROM CHANGETABLE(CHANGES '' + QUOTENAME(OBJECT_SCHEMA_NAME(st.object_id)) + ''.'' + QUOTENAME(st.NAME) + '', @last_synchronization_version) AS CT '' + ''CROSS APPLY ( select '' + fullColumnList + '' from '' + QUOTENAME(OBJECT_SCHEMA_NAME(st.object_id)) + ''.'' + QUOTENAME(st.NAME) + '' T where '' + indexJoinCondition + ''  FOR XML RAW(''''TR'''') , ELEMENTS, BINARY BASE64) xmlData(xmlRowData)''
FROM sys.change_tracking_tables ctt
INNER JOIN sys.tables st ON ctt.object_id = st.object_id
INNER JOIN sys.indexes si ON ctt.object_id = si.object_id
	AND si.is_primary_key = 1
CROSS APPLY (
	SELECT ''<TR>'' + (
			SELECT c.NAME [TH]
			FROM sys.columns AS c
			WHERE st.object_id = c.object_id
				AND c.system_type_id NOT IN (
					34
					,35
					,99
					,189
					,241
					)
				AND c.max_length <> - 1
			ORDER BY c.column_id ASC
			FOR XML RAW('''')
				,ELEMENTS
			) + ''</TR>''
	) headerColumnsData(headerColumnsHtml)
CROSS APPLY (
	SELECT  REPLACE(REPLACE(STUFF((
				SELECT ''AND CT.'' + QUOTENAME(COL_NAME(sc.object_id, sc.column_id)) + ''= T.'' + QUOTENAME(COL_NAME(sc.object_id, sc.column_id)) AS [text()]
				FROM sys.index_columns AS sc
				WHERE sc.object_id = si.object_id
					AND sc.index_id = si.index_id
					AND sc.is_included_column = 0
				ORDER BY sc.index_column_id ASC
				FOR XML PATH('''')
				), 1, 3, ''''),''&lt;'',''<''),''&gt;'',''>'')
	) joinConditionData(indexJoinCondition)
CROSS APPLY (
	SELECT  REPLACE(REPLACE(STUFF((
				SELECT '',CT.'' + QUOTENAME(COL_NAME(sc.object_id, sc.column_id)) AS [text()]
				FROM sys.index_columns AS sc
				WHERE sc.object_id = si.object_id
					AND sc.index_id = si.index_id
					AND sc.is_included_column = 0
				ORDER BY sc.index_column_id ASC
				FOR XML PATH('''')
				), 1, 1, ''''),''&lt;'',''<''),''&gt;'',''>'')
	) columnsData(indexColumns)
CROSS APPLY (
	SELECT REPLACE(REPLACE(STUFF((
				SELECT '', CASE WHEN CHANGE_TRACKING_IS_COLUMN_IN_MASK('' + cast(c.column_id AS VARCHAR(3)) + '', CT.SYS_CHANGE_COLUMNS) = 1 then isnull(cast(T.'' + QUOTENAME(c.NAME) + '' as nvarchar(max)),''''null'''') ELSE NULL END [TDColor]'' + '', CASE WHEN CHANGE_TRACKING_IS_COLUMN_IN_MASK('' + cast(c.column_id AS VARCHAR(3)) + '', CT.SYS_CHANGE_COLUMNS) = 0 then isnull(cast(T.'' + QUOTENAME(c.NAME) + ''  as nvarchar(max)),''''null'''') ELSE NULL END [TD]'' AS [text()]
				FROM sys.columns AS c
				WHERE st.object_id = c.object_id
					AND c.system_type_id NOT IN (
						34
						,35
						,99
						,189
						,241
						)
					AND c.max_length <> - 1
				ORDER BY c.column_id ASC
				FOR XML PATH('''')
					,ELEMENTS
				), 1, 2, ''''),''&lt;'',''<''),''&gt;'',''>'')
	) fullColumnListData(fullColumnList)
' + @LikeCondition;

PRINT @SqlStatement

SET @SqlStatement = REPLACE(@SqlStatement,'#DatabaseName#',QUOTENAME(@DatabaseName));
EXEC(@SqlStatement)

