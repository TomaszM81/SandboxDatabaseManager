CREATE PROCEDURE [dbo].[CompareDataInTable]
(
    @FirstDatabaseName NVARCHAR(128),
    @SecondDatabaseName NVARCHAR(128),
    @TableName NVARCHAR(200)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

declare @SqlStatement NVARCHAR(MAX) = '',
        @primary_key_columns NVARCHAR(MAX) = '',
	   @primary_key_columns_values NVARCHAR(MAX) = '',
	   @primary_key_columns_with_table NVARCHAR(MAX) = '',
	   @checksum_columns NVARCHAR(MAX) = '',
	   @index_id INT,
        @join_condition NVARCHAR(MAX) = '',
	   @primary_key_columns_definition NVARCHAR(MAX) = '',
	   @header NVARCHAR(MAX) = '<th>Database Name</th>',
	   @columns NVARCHAR(MAX) = '',
	   @columns_diff NVARCHAR(MAX) = '',
	   @checksum_agg_FirstDatabaseName INT,
	   @checksum_agg_SecondDatabaseName INT


SET @SqlStatement = ' USE ' +  QUOTENAME(@FirstDatabaseName) + '; set @index_id = (select index_id from sys.indexes where is_primary_key = 1 and object_id = object_id(@TableName))'
EXEC sp_executesql	@SqlStatement,
				N'@index_id INT OUTPUT, @TableName NVARCHAR(200)',
				@index_id = @index_id OUTPUT,
				@TableName = @TableName

IF @index_id IS NULL
    RAISERROR('%s table is missing primary key constraint, data comparison will not be possible.',11,0, @TableName);


SET @SqlStatement = ' USE ' +  QUOTENAME(@FirstDatabaseName) + '

    SELECT 
	   @primary_key_columns +=  '', '' + QUOTENAME(COL_NAME(object_id, column_id)),
	   @primary_key_columns_values += '' + ''''</br>'' + QUOTENAME(COL_NAME(object_id, column_id)) + '' = '''' + CAST('' + QUOTENAME(COL_NAME(object_id, column_id)) + '' AS NVARCHAR(MAX))'',
	   @primary_key_columns_with_table += '', coalesce(#FirstDatabase.'' + QUOTENAME(COL_NAME(object_id, column_id)) + '', #SecondDatabase.'' + QUOTENAME(COL_NAME(object_id, column_id)) + '') '' + QUOTENAME(COL_NAME(object_id, column_id)),
	   @join_condition += '' AND #FirstDatabase.'' + QUOTENAME(COL_NAME(object_id, column_id)) + '' = #SecondDatabase.'' + QUOTENAME(COL_NAME(object_id, column_id))
     FROM sys.index_columns WHERE object_id = OBJECT_ID(@TableName) AND index_id = @index_id ORDER BY index_column_id ASC

    SELECT  
	   @checksum_columns += '', '' + QUOTENAME(sc.name)
    FROM sys.columns sc WHERE sc.OBJECT_ID = OBJECT_ID(@TableName) AND system_type_id NOT IN (34,35,98,99,189,241) AND max_length <> -1 order by column_id asc

    SELECT 
	   @primary_key_columns_definition += '', '' + QUOTENAME(sc.name) + '' '' + ISNULL(t.name,t2.name) + 
								     CASE 
								  	  WHEN COALESCE(t.system_type_id, t2.system_type_id) IN (239,231,175,167,165) THEN ''('' + CAST(sc.max_length AS VARCHAR(10)) + '')''
								  	  WHEN COALESCE(t.system_type_id, t2.system_type_id) IN (106,108) THEN ''('' + CAST(sc.precision AS VARCHAR(10)) + '','' + CAST(sc.scale as VARCHAR(10)) + '')''
	 								  ELSE ''''
								     END + '' NOT NULL'' 
	 FROM sys.index_columns AS ic
	 INNER JOIN sys.indexes si ON ic.object_id = si.object_id and ic.index_id = si.index_id
	 INNER JOIN sys.columns sc ON ic.object_id = sc.object_id and ic.column_id = sc.column_id
	 LEFT JOIN sys.types AS t on sc.system_type_id = t.system_type_id and sc.user_type_id = t.user_type_id AND t.is_user_defined = 0 and t.user_type_id not in (256)
	 LEFT JOIN sys.types AS t2 on sc.system_type_id = t2.system_type_id and t2.is_user_defined = 0 and t2.user_type_id = t2.system_type_id
	 WHERE si.object_id = object_id(@TableName) AND si.is_primary_key = 1 order by ic.column_id asc
								  
	SELECT 
	   @header += ''<th>'' + replace(replace(name,''<'',''&lt''),''>'',''&gt'') + ''</th>'',
	   @columns += '', isnull(cast('' + QUOTENAME(name) + '' as nvarchar(max)),''''null'''') [td]'',
	   @columns_diff += '', nullif(isnull(cast(FirstTable.''+ quotename(name) +'' as nvarchar(max)),''''null''''), isnull(cast(SecondTable.'' + quotename(name) + '' as nvarchar(max)),''''null'''')) [TDColor], CASE WHEN isnull(cast(FirstTable.''+ quotename(name) + ''  as nvarchar(max)),''''null'''') = isnull(cast(SecondTable.'' + quotename(name)+ ''  as nvarchar(max)),''''null'''') then isnull(cast(FirstTable.''+ quotename(name) + '' as nvarchar(max)),''''null'''') else NULL end [td]'' 
	FROM sys.columns 
	WHERE 
	   object_id = object_id(@TableName) 
	   AND system_type_id NOT IN (34,35,98,99,189,241) AND max_length <> -1
	ORDER by column_id asc'


	

 EXEC sp_executesql @SqlStatement, N'@index_id INT, @TableName NVARCHAR(200), @primary_key_columns NVARCHAR(MAX) out, @primary_key_columns_with_table NVARCHAR(MAX) out, @join_condition NVARCHAR(MAX) out, @checksum_columns NVARCHAR(MAX) out, @primary_key_columns_definition NVARCHAR(MAX) out, @header NVARCHAR(MAX) out, @columns NVARCHAR(MAX) out, @columns_diff NVARCHAR(MAX) out, @primary_key_columns_values NVARCHAR(MAX) out'
				, @primary_key_columns = @primary_key_columns OUT, @join_condition = @join_condition OUT, @checksum_columns = @checksum_columns OUT, @primary_key_columns_definition = @primary_key_columns_definition OUT, @TableName = @TableName, @index_id = @index_id, @primary_key_columns_with_table = @primary_key_columns_with_table OUT, @header = @header OUT, @columns = @columns OUT, @columns_diff = @columns_diff OUT, @primary_key_columns_values = @primary_key_columns_values out


 SET @primary_key_columns_values = STUFF(@primary_key_columns_values,5,5,'')
 SET @primary_key_columns_values = STUFF(@primary_key_columns_values,1,2,'')
 SET @primary_key_columns = STUFF(@primary_key_columns,1,1,'')
 SET @primary_key_columns_with_table = STUFF(@primary_key_columns_with_table,1,1,'')
 SET @join_condition = STUFF(@join_condition,1,4,'') 
 SET @columns = STUFF(@columns,1,1,'')
 SET @columns_diff = STUFF(@columns_diff,1,1,'')
 SET @checksum_columns = STUFF(@checksum_columns,1,1,'')
 SET @primary_key_columns_definition = STUFF(@primary_key_columns_definition,1,1,'')
       
 SET @SqlStatement =  N'set @checksum_agg_FirstDatabaseName = (select checksum_agg( checksum ( ' + @checksum_columns + ' )) from ' +  QUOTENAME(@FirstDatabaseName) + '.' + @TableName + ')'
 EXEC sp_executesql @SqlStatement, N'@checksum_agg_FirstDatabaseName int OUT', @checksum_agg_FirstDatabaseName = @checksum_agg_FirstDatabaseName OUT
 SET @SqlStatement =  N'set @checksum_agg_SecondDatabaseName = (select checksum_agg( checksum ( ' + @checksum_columns + ' )) from ' +  QUOTENAME(@SecondDatabaseName) + '.' + @TableName + ')'
 EXEC sp_executesql @SqlStatement, N'@checksum_agg_SecondDatabaseName int OUT', @checksum_agg_SecondDatabaseName = @checksum_agg_SecondDatabaseName OUT




  IF(@checksum_agg_SecondDatabaseName = @checksum_agg_FirstDatabaseName)
    RETURN 0




 SET @SqlStatement = '

  CREATE TABLE #FirstDatabase(DatabaseName NVARCHAR(100) NOT NULL, ' + @primary_key_columns_definition + ', RowCheckSum INT NOT NULL, PRIMARY KEY (' + @primary_key_columns + ')) 
  CREATE TABLE #SecondDatabase(DatabaseName NVARCHAR(100) NOT NULL, ' + @primary_key_columns_definition + ', RowCheckSum INT NOT NULL, PRIMARY KEY (' + @primary_key_columns + '))
  
  insert #FirstDatabase select ''' + @FirstDatabaseName + ''',' + @primary_key_columns + ', checksum ( ' + @checksum_columns + ') from ' + QUOTENAME(@FirstDatabaseName) + '.' + @TableName + ' 
  insert #SecondDatabase select ''' + @SecondDatabaseName + ''',' + @primary_key_columns + ', checksum ( ' + @checksum_columns + ') from ' + QUOTENAME(@SecondDatabaseName) + '.' + @TableName + ' 



  select top 100 ''' + @TableName + ''' TableName,' + @primary_key_columns_values + ' TableKeyValues, RowDifference RowDifferenceType,
   ''<table id="row_result"><tr>' +  @header + '</tr>'' + coalesce( REPLACE(row_diff_1.row_data + row_diff_2.row_data,''TDColor>'',''TD class="row_diff">''), row_additional.row_data, row_missing.row_data) + ''</table>'' RowData 
  FROM 
  (
	 SELECT ' + @primary_key_columns_with_table + '
	   ,case 
		  when #FirstDatabase.RowCheckSum is null then ''Missing Row''
		  when #SecondDatabase.RowCheckSum is null then ''Additional Row''
		  else ''Row Difference''
	    end RowDifference
	   from #FirstDatabase full join #SecondDatabase on ' + @join_condition + '
	   WHERE #FirstDatabase.RowCheckSum <> #SecondDatabase.RowCheckSum OR #FirstDatabase.RowCheckSum IS null OR #SecondDatabase.RowCheckSum IS null
  ) data
  outer apply
  (
    select ''' + @FirstDatabaseName + ''' [td], ' + @columns_diff + '
    from ' + QUOTENAME(@FirstDatabaseName) + '.' + @TableName + ' FirstTable inner join ' + QUOTENAME(@SecondDatabaseName) + '.' + @TableName + ' SecondTable on ' +  REPLACE(REPLACE(@join_condition, '#FirstDatabase', 'FirstTable'),'#SecondDatabase', 'SecondTable') + '
    where RowDifference = ''Row Difference'' and ' + REPLACE(REPLACE(@join_condition, '#FirstDatabase', 'FirstTable'),'#SecondDatabase', 'data')  + '
    for xml raw(''tr''), elements, BINARY BASE64
  )row_diff_1(row_data)
  outer apply
  (
    select ''' + @SecondDatabaseName + ''' [td], ' + @columns_diff + '
    from ' + QUOTENAME(@SecondDatabaseName) + '.' + @TableName + ' FirstTable inner join ' + QUOTENAME(@FirstDatabaseName) + '.' + @TableName + ' SecondTable on ' +  REPLACE(REPLACE(@join_condition, '#FirstDatabase', 'FirstTable'),'#SecondDatabase', 'SecondTable') + '
    where RowDifference = ''Row Difference'' and ' + REPLACE(REPLACE(@join_condition, '#FirstDatabase', 'FirstTable'),'#SecondDatabase', 'data') + '
    for xml raw(''tr''), elements, BINARY BASE64
  )row_diff_2(row_data)
  outer apply
  (
    select ''' + @SecondDatabaseName + ''' [td], ' + @columns + '
    from ' + QUOTENAME(@SecondDatabaseName) + '.' + @TableName + ' FirstTable 
    where RowDifference = ''Missing Row'' and ' + REPLACE(REPLACE(@join_condition, '#FirstDatabase', 'FirstTable'),'#SecondDatabase', 'data') + '
    for xml raw(''tr''), elements, BINARY BASE64
  )row_missing(row_data)
  outer apply
  (
    select ''' + @FirstDatabaseName + ''' [td], ' + @columns + '
    from ' + QUOTENAME(@FirstDatabaseName) + '.' + @TableName + ' FirstTable 
    where RowDifference = ''Additional Row'' and ' + REPLACE(REPLACE(@join_condition, '#FirstDatabase', 'FirstTable'),'#SecondDatabase', 'data') + '
    for xml raw(''tr''), elements, BINARY BASE64
  )row_additional(row_data)
  order by ' + @primary_key_columns

  EXEC(@SqlStatement)
