CREATE PROCEDURE [dbo].[InsertTaskHistory]
(
    @ID CHAR(36),
	@Name NVARCHAR(MAX),
    @Type CHAR(1) = null,
    @OutputText NVARCHAR(MAX) = null,
    @Owner NVARCHAR(100),
    @Result NVARCHAR(MAX) = null,
    @Status INT,
    @RedirectToController NVARCHAR(100) = null,
    @RedirectToAction NVARCHAR(100) = null,
    @Description NVARCHAR(MAX) = null,
    @StartDate DATETIME2(0),
    @EndDate DATETIME2(0)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DELETE data FROM
(
	select RowNum = ROW_NUMBER() OVER (ORDER BY EndDate desc) from dbo.TaskHistory WHERE Owner = @Owner
) data where RowNum >= 15;


INSERT INTO dbo.TaskHistory(ID, Name, Type, OutputText, Owner, Result, Status, RedirectToController, RedirectToAction, [Description], StartDate, EndDate)
VALUES (@ID, @Name ,@Type, @OutputText, @Owner, @Result, @Status, @RedirectToController, @RedirectToAction, @Description, @StartDate, @EndDate);
