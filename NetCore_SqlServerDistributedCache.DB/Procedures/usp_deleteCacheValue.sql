CREATE PROCEDURE dbo.usp_deleteCacheValue
(
	@Key VARCHAR(900)
)
AS
BEGIN
	DELETE dbo.AppCache
	WHERE AppCacheKey = @Key;
END