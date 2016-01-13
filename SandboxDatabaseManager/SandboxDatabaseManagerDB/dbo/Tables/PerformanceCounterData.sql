CREATE TABLE [dbo].[PerformanceCounterData] (
    [ServerFriendlyNameID]  SMALLINT      NOT NULL,
    [CounterFriendlyNameID] SMALLINT      NOT NULL,
    [CounterValueDate]      DATETIME2 (0) NOT NULL,
    [CounterValue]          FLOAT (53)    NOT NULL,
    PRIMARY KEY CLUSTERED ([ServerFriendlyNameID] ASC, [CounterFriendlyNameID] ASC, [CounterValueDate] ASC),
    FOREIGN KEY ([CounterFriendlyNameID]) REFERENCES [dbo].[CounterFriendlyName] ([ID]),
    FOREIGN KEY ([ServerFriendlyNameID]) REFERENCES [dbo].[ServerFriendlyName] ([ID])
);

