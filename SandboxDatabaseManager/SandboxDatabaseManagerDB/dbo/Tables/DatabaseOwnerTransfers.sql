CREATE TABLE [dbo].[DatabaseOwnerTransfers] (
    [ID]               INT            IDENTITY (1, 1) NOT NULL,
    [DatabaseName]     NVARCHAR (128) NOT NULL,
    [OldDatabaseOwner] NVARCHAR (128) NOT NULL,
    [TransferDate]     DATETIME2 (7)  DEFAULT (sysdatetime()) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_DatabaseOwnerTransfers_Databases] FOREIGN KEY ([DatabaseName]) REFERENCES [dbo].[Databases] ([DatabaseName]) ON DELETE CASCADE,
    CONSTRAINT [UK_DatabaseOwnerTransfers] UNIQUE NONCLUSTERED ([DatabaseName] ASC, [OldDatabaseOwner] ASC, [TransferDate])
);

