CREATE PROCEDURE [dbo].[GetTableDataChanges]
(
	@DatabaseName nvarchar(128),
	@SlqStatement nvarchar(max),
	@Revision int
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
SET @SlqStatement = 'USE ' + QUOTENAME(@DatabaseName) + ';' + @SlqStatement;
exec sp_executesql @SlqStatement, N'@last_synchronization_version int', @last_synchronization_version = @Revision
