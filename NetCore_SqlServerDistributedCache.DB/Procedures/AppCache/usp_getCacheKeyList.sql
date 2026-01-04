CREATE PROCEDURE dbo.usp_getCacheKeyList
AS
BEGIN
	SELECT 
		ac.AppCacheKey
	FROM dbo.AppCache ac;
END
