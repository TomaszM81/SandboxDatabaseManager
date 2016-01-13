CREATE TABLE [dbo].[Configuration](
	[Constraint] as 1 PRIMARY KEY,
	[RestorePathData] [nvarchar](255) NULL,
	[RestorePathLog] [nvarchar](255) NULL,
	[VersionColumnSchemaTemplate] [nvarchar](max) NULL,
	[VersionColumnUpdateStatementPart] [nvarchar](max) NULL,
	[SetRecoveryModelToSimpleDefault] bit NULL
)