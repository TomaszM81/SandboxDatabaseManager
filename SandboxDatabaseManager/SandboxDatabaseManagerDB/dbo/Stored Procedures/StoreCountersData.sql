CREATE PROCEDURE dbo.StoreCountersData
(
	@CountersToStore dbo.CountersData READONLY
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

INSERT INTO dbo.ServerFriendlyName
SELECT DISTINCT ServerFriendlyName FROM @CountersToStore C
WHERE NOT EXISTS (SELECT 1 FROM dbo.ServerFriendlyName WHERE dbo.ServerFriendlyName.ServerFriendlyName = C.ServerFriendlyName);

INSERT INTO dbo.CounterFriendlyName
SELECT DISTINCT CounterFriendlyName FROM @CountersToStore C
WHERE NOT EXISTS (SELECT 1 FROM dbo.CounterFriendlyName WHERE dbo.CounterFriendlyName.CounterFriendlyName = C.CounterFriendlyName);


INSERT dbo.PerformanceCounterData (ServerFriendlyNameID, CounterFriendlyNameID, CounterValueDate, CounterValue)
SELECT sfn.ID, cfn.ID, C.CounterValueDate, C.CounterValue  
FROM @CountersToStore C
INNER JOIN dbo.ServerFriendlyName AS sfn on C.ServerFriendlyName = sfn.ServerFriendlyName
INNER JOIN dbo.CounterFriendlyName AS cfn on C.CounterFriendlyName = cfn.CounterFriendlyName;
