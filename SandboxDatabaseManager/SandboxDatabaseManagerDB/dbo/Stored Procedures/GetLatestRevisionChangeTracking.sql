CREATE PROCEDURE [dbo].[GetLatestRevisionChangeTracking]
(
	@DatabaseName NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @SqlStatement NVARCHAR(max) = 'USE ' + quotename(@DatabaseName) + '; ' + 'SELECT CHANGE_TRACKING_CURRENT_VERSION() Revision';
EXEC(@SqlStatement)
