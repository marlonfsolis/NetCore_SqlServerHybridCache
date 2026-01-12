CREATE PROCEDURE dbo.usp_setCacheValue
(
	@Key VARCHAR(900),
	@Value VARBINARY(MAX),
	@AbsoluteExpiration DATETIME2(7),
    @DataType VARCHAR(100)
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
           ,TrackingNo = TrackingNo + 1
           ,DataType = @DataType
        WHERE AppCacheKey = @Key;
    END
    ELSE BEGIN
        INSERT INTO dbo.AppCache (AppCacheKey, CacheValue, AbsoluteExpiration, TrackingNo, DataType)
	    VALUES (@Key, @Value, @AbsoluteExpiration, 1, @DataType);
    END
END