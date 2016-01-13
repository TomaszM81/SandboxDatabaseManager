CREATE TYPE [dbo].[CountersData] AS TABLE (
    [ServerFriendlyName]  NVARCHAR (100) NOT NULL,
    [CounterFriendlyName] NVARCHAR (100) NOT NULL,
    [CounterValue]        FLOAT (53)     NOT NULL,
    [CounterValueDate]    NVARCHAR (100) NOT NULL,
    PRIMARY KEY CLUSTERED ([ServerFriendlyName] ASC, [CounterFriendlyName] ASC, [CounterValueDate] ASC));

