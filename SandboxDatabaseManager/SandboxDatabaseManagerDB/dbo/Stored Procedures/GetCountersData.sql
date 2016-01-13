CREATE PROCEDURE dbo.GetCountersData
(
    @ServerFriendlyName NVARCHAR(100),
    @CounterFriendlyName NVARCHAR(100),
    @DateStart DATETIME2(0) = null,
    @DateEnd DATETIME2(0) = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

SELECT sfn.ServerFriendlyName, cfn.CounterFriendlyName, pcd.CounterValue, pcd.CounterValueDate 
FROM dbo.PerformanceCounterData AS pcd
INNER JOIN dbo.CounterFriendlyName AS cfn ON pcd.CounterFriendlyNameID = cfn.ID
INNER JOIN dbo.ServerFriendlyName AS sfn ON pcd.ServerFriendlyNameID = sfn.ID
WHERE 
    sfn.ServerFriendlyName = @ServerFriendlyName
    AND cfn.CounterFriendlyName = @CounterFriendlyName
    AND pcd.CounterValueDate BETWEEN COALESCE(@DateStart,CounterValueDate) and COALESCE(@DateEnd,CounterValueDate)
ORDER BY pcd.CounterValueDate ASC
