CREATE PROCEDURE [dbo].[ExecuteReadOnlyStatementAtDatabase]
(
	@SqlStatement NVARCHAR(MAX),
	@DatabaseName NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @ExecuteTemplate AS nvarchar(max)
DECLARE @FinalSqlStatement AS nvarchar(max)

SET @ExecuteTemplate = 'USE ' + quotename(@DatabaseName) + ' 
IF NOT EXISTS (SELECT * FROM sys.database_principals dp WHERE name = ''reader_ExecuteStatementProxy'')
	CREATE USER [reader_ExecuteStatementProxy] WITHOUT LOGIN;

EXEC sp_addrolemember ''db_datareader'', ''reader_ExecuteStatementProxy'';

SET ROWCOUNT 10;
EXEC(@SqlStatement) AS USER = ''reader_ExecuteStatementProxy'''

EXEC sp_executesql @ExecuteTemplate, N'@SqlStatement NVARCHAR(MAX)', @SqlStatement = @SqlStatement;
