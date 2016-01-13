CREATE PROCEDURE [dbo].[DeleteOldCounterData]
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DELETE data from 
(
	SELECT CounterValueDate, MAX(CounterValueDate) OVER(PARTITION BY ServerFriendlyNameID, CounterFriendlyNameID) LatestCounterDate FROM dbo.PerformanceCounterData
) data WHERE DATEDIFF(day,data.CounterValueDate, LatestCounterDate) > 60

DELETE FROM dbo.ServerFriendlyName WHERE NOT EXISTS (SELECT 1 FROM dbo.PerformanceCounterData WHERE ServerFriendlyNameID = ID);
DELETE FROM dbo.CounterFriendlyName WHERE NOT EXISTS (SELECT 1 FROM dbo.PerformanceCounterData WHERE CounterFriendlyNameID = ID);
