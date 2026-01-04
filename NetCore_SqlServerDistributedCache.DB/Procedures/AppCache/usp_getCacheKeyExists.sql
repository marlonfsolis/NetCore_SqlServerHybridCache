CREATE PROCEDURE dbo.usp_getCacheKeyExists 
(
	@Key VARCHAR(900)
)
AS
BEGIN
	DECLARE @Exists BIT = 0;

	IF EXISTS (
		SELECT 1
		FROM dbo.AppCache ac
		WHERE ac.AppCacheKey = @Key
	) BEGIN
		SET @Exists = 1;
	END

	SELECT @Exists AS 'KeyExists';
END