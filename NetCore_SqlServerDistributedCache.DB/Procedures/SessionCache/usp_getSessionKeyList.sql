CREATE PROCEDURE dbo.usp_getSessionKeyList
(
	@Id VARCHAR(449)
)
AS
BEGIN
	SELECT 
		scv.SessionKey
	FROM dbo.SessionCacheValue scv
	WHERE scv.SessionId = @Id;
END
