CREATE TABLE [dbo].[TaskHistory] (
    [ID]                   CHAR (36)      NOT NULL,
    [Name]                 NVARCHAR (MAX) NOT NULL,
    [Type]                 CHAR (1)       NULL,
    [OutputText]           NVARCHAR (MAX) NULL,
    [Owner]                NVARCHAR (100) NOT NULL,
    [Result]               NVARCHAR (MAX) NULL,
    [Status]               INT            NOT NULL,
    [RedirectToController] NVARCHAR (100) NULL,
    [RedirectToAction]     NVARCHAR (100) NULL,
    [Description]          NVARCHAR (MAX) NULL,
    [StartDate]            DATETIME2 (0)  NULL,
    [EndDate]              DATETIME2 (0)  NULL,
    PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [idx_TaskHistory]
    ON [dbo].[TaskHistory]([Owner] ASC, [EndDate] ASC);

