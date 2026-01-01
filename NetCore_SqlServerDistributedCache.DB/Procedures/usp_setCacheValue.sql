CREATE PROCEDURE dbo.usp_setCacheValue
(
	@Key VARCHAR(900),
	@Value VARBINARY(MAX),
	@AbsoluteExpiration DATETIMEOFFSET
)
AS
BEGIN
	IF EXISTS(
    	SELECT 1
    	FROM dbo.AppCache ac
        WHERE ac.AppCacheKey = @Key
    ) BEGIN
        UPDATE AppCache 
        SET CacheValue = @Value
           ,AbsoluteExpiration = @AbsoluteExpiration
        WHERE AppCacheKey = @Key;
    END
    ELSE BEGIN
        INSERT INTO dbo.AppCache (AppCacheKey, CacheValue, AbsoluteExpiration)
	    VALUES (@Key, @Value, @AbsoluteExpiration);
    END
END