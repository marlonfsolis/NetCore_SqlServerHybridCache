CREATE PROCEDURE dbo.usp_getSessionKeyExists 
(
	@Id NVARCHAR(449),
	@Key VARCHAR(1000)
)
AS
BEGIN
	DECLARE @Exists BIT = 0;

	IF EXISTS (
		SELECT 1
		FROM dbo.SessionCache sc
		WHERE sc.SessionId = @Id
		AND sc.SessionKey = @Key
	) BEGIN
		SET @Exists = 1;
	END

	SELECT @Exists AS 'KeyExists';
END