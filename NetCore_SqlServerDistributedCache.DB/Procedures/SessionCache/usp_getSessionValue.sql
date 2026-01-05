CREATE PROCEDURE dbo.usp_getSessionValue
(
	@Id VARCHAR(449),
	@Key VARCHAR(800),
	@UtcNow DATETIMEOFFSET
)
AS
BEGIN
	SELECT
		scv.SessionValue
	FROM dbo.SessionCache sc
	INNER JOIN dbo.SessionCacheValue scv ON sc.SessionId = scv.SessionId
	WHERE sc.SessionId = @Id 
	AND scv.SessionKey = @Key
	AND sc.AbsoluteExpiration >= @UtcNow;
END