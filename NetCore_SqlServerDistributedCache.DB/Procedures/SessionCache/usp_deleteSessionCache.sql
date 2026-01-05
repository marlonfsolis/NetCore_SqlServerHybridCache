CREATE PROCEDURE dbo.usp_deleteSessionCache
(
	@Id VARCHAR(449)
)
AS
BEGIN
	DELETE dbo.SessionCacheValue
	WHERE SessionId = @Id;
	
	DELETE dbo.SessionCache
	WHERE SessionId = @Id;
END
