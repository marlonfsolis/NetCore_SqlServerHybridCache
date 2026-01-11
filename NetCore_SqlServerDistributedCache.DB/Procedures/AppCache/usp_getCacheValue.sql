CREATE PROCEDURE dbo.usp_getCacheValue
(
	@Key VARCHAR(900),
	@UtcNow DATETIME2(7)
)
AS
BEGIN
	SELECT
		CacheValue
	FROM dbo.AppCache
	WHERE AppCacheKey = @Key
	AND AbsoluteExpiration >= @UtcNow;
END