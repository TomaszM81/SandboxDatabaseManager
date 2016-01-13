CREATE TABLE [dbo].[Users] (
    [ID]                                                   INT            IDENTITY (1, 1) NOT NULL,
    [DomainUserName]                                       NVARCHAR (128) NOT NULL,
    [PrimaryOnlyHasSandboxDatabaseManagerAccessPermission] BIT            DEFAULT ((0)) NOT NULL, -- has access to the Tool (Web Interface), does not mean a user can restore any database
    [PrimaryOnlyHasOverwriteBackupDestinationPermission]   BIT            DEFAULT ((0)) NOT NULL, -- is able to manually input path for a new database backup file
    [HasCopyAndSearchFromPermission]                       BIT            DEFAULT ((0)) NOT NULL, -- is able to copy and search databases on this server, this is a prerequisite for HasBackupFromPermission as the server will not be listed on the database selection pages
    [HasBackupFromPermission]                              BIT            DEFAULT ((0)) NOT NULL, -- is able to take backups of databases on this server
    [HasRestoreFromPermission]                             BIT            DEFAULT ((0)) NOT NULL, -- is able to restore from backup files on this server
    [HasRestoreToPermission]                               BIT            DEFAULT ((0)) NOT NULL, -- is able to restore databases on this server
    [HasBackupToPermission]                                BIT            DEFAULT ((0)) NOT NULL, -- is able to take backups of databases on this server
    PRIMARY KEY CLUSTERED ([ID] ASC),
    UNIQUE NONCLUSTERED ([DomainUserName] ASC)
);

