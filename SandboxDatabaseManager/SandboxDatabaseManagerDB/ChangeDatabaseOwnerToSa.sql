ALTER AUTHORIZATION ON DATABASE::[$(DatabaseName)] TO sa
GO

IF EXISTS(SELECT * FROM sys.extended_properties WHERE name = N'Version' AND class = 0)
BEGIN
    EXEC sys.sp_updateextendedproperty @name=N'Version', @value=N'1.0' 
END
ELSE
BEGIN
   EXEC sys.sp_addextendedproperty @name=N'Version', @value=N'1.0' 
END
GO