CREATE PROCEDURE dbo.usp_clearCache
AS
BEGIN
    DELETE FROM dbo.AppCache;
END
