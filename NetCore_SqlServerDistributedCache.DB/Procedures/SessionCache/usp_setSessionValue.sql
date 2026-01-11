CREATE PROCEDURE dbo.usp_setSessionValue
(
    @Id VARCHAR(449),
	@Key VARCHAR(800),
	@Value VARBINARY(MAX),
	@AbsoluteExpiration DATETIME2(7),
    @DataType VARCHAR(1000)
)
AS
BEGIN
    -- Session Cache
    IF EXISTS (
        SELECT 1
        FROM dbo.SessionCache sc
        WHERE sc.SessionId = @Id
    ) BEGIN
        UPDATE dbo.SessionCache 
        SET AbsoluteExpiration = @AbsoluteExpiration
        WHERE SessionId = @Id;
    END
    ELSE BEGIN
        INSERT INTO dbo.SessionCache (SessionId, AbsoluteExpiration)
	    VALUES (@Id, @AbsoluteExpiration);
    END

    -- Session Cache Value
	IF EXISTS(
    	SELECT 1
    	FROM dbo.SessionCacheValue scv
        WHERE scv.SessionId = @Id
        AND scv.SessionKey = @Key
    ) BEGIN
        UPDATE SessionCacheValue
        SET SessionValue = @Value
           ,TrackingNo = TrackingNo + 1
           ,DataType = @DataType
        WHERE SessionId = @Id
        AND SessionKey = @Key;
    END
    ELSE BEGIN
        INSERT INTO dbo.SessionCacheValue (SessionId, SessionKey, SessionValue, TrackingNo, DataType)
	    VALUES (@Id, @Key, @Value, 0, @DataType);
    END
END