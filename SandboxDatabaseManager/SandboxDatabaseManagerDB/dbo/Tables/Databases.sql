CREATE TABLE [dbo].[Databases] (
    [ID]                         INT            IDENTITY (1, 1) NOT NULL,
    [DatabaseName]               NVARCHAR (128) NOT NULL,
    [DatabaseOwner]              NVARCHAR (128) NOT NULL,
    [OriginatingServer]          NVARCHAR (128) NOT NULL,
    [OriginatingDatabaseName]    NVARCHAR (128) NOT NULL,
    [SourceBackupFileCollection] XML            NULL,
    [Comment]                    NVARCHAR (MAX) NULL,
    [RestoredOn]                 DATETIME2 (7)  NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    UNIQUE NONCLUSTERED ([DatabaseName] ASC)
);

