CREATE PROCEDURE dbo.GetDateRangeForCounter
(
    @ServerFriendlyName NVARCHAR(100),
    @CounterFriendlyName NVARCHAR(100)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

SELECT MIN(pcd.CounterValueDate) MinCounterValueDate, MAX(pcd.CounterValueDate) MaxCounterValueDate
FROM dbo.PerformanceCounterData AS pcd
INNER JOIN dbo.CounterFriendlyName AS cfn ON pcd.CounterFriendlyNameID = cfn.ID
INNER JOIN dbo.ServerFriendlyName AS sfn ON pcd.ServerFriendlyNameID = sfn.ID
WHERE 
    sfn.ServerFriendlyName = @ServerFriendlyName
    AND cfn.CounterFriendlyName = @CounterFriendlyName
  
