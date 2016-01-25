CREATE PROCEDURE [dbo].[GetCountersDataMinDates]
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

SELECT sfn.ServerFriendlyName, cfn.CounterFriendlyName, (SELECT TOP 1 CounterValueDate FROM dbo.PerformanceCounterData AS pcd WHERE pcd.ServerFriendlyNameID = sfn.ID AND pcd.CounterFriendlyNameID = cfn.ID ORDER by pcd.CounterValueDate asc ) MinCounterValueDate 
FROM dbo.CounterFriendlyName AS cfn
CROSS JOIN dbo.ServerFriendlyName AS sfn
