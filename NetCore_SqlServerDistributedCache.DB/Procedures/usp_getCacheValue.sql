CREATE PROCEDURE usp_getCacheValue
(
	@Key VARCHAR(900),
	@UtcNow DATETIMEOFFSET
)
AS
BEGIN
	SELECT
		CacheValue
	FROM dbo.AppCache
	WHERE AppCacheKey = @Key
	AND AbsoluteExpiration >= @UtcNow;
END