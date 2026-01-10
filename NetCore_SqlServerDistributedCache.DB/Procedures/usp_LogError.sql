--
-- Create an error log entry.
--
-- DROP PROCEDURE IF EXISTS dbo.usp_logError
--
CREATE PROCEDURE dbo.usp_logError
(
    @ErrorMessage NVARCHAR(MAX),
    @ErrorDetail NVARCHAR(MAX)
)
AS
BEGIN
    INSERT INTO dbo.ErrorLog (ErrorMessage, ErrorDetail, ErrorDate)
	VALUES (@ErrorMessage, @ErrorDetail, SYSDATETIME());
END
