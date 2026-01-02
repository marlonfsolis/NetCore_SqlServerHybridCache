CREATE PROCEDURE dbo.usp_deleteCacheValue
(
	@Key VARCHAR(900)
)
AS
BEGIN
	UPDATE dbo.AppCache 
	SET CacheValue = NULL
	   ,TrackingNo = TrackingNo + 1
	WHERE AppCacheKey = @Key;
END