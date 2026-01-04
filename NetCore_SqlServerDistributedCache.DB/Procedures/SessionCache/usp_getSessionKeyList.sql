CREATE PROCEDURE dbo.usp_getSessionKeyList
(
	@Id NVARCHAR(449)
)
AS
BEGIN
	SELECT 
		sc.SessionKey
	FROM dbo.SessionCache sc
	WHERE sc.SessionId = @Id;
END
