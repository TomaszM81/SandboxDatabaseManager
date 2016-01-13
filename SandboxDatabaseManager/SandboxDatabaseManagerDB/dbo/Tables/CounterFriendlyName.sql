CREATE TABLE [dbo].[CounterFriendlyName] (
    [ID]                  SMALLINT       IDENTITY (1, 1) NOT NULL,
    [CounterFriendlyName] NVARCHAR (100) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    UNIQUE NONCLUSTERED ([CounterFriendlyName] ASC)
);

