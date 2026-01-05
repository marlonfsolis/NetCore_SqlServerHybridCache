CREATE PROCEDURE dbo.usp_getSessionKeyExists 
(
	@Id VARCHAR(449),
	@Key VARCHAR(800)
)
AS
BEGIN
	DECLARE @Exists BIT = 0;

	IF EXISTS (
		SELECT 1
		FROM dbo.SessionCacheValue scv
		WHERE scv.SessionId = @Id
		AND scv.SessionKey = @Key
	) BEGIN
		SET @Exists = 1;
	END

	SELECT @Exists AS 'KeyExists';
END