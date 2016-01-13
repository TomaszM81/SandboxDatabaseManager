CREATE TABLE [dbo].[ServerFriendlyName] (
    [ID]                 SMALLINT       IDENTITY (1, 1) NOT NULL,
    [ServerFriendlyName] NVARCHAR (100) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    UNIQUE NONCLUSTERED ([ServerFriendlyName] ASC)
);

