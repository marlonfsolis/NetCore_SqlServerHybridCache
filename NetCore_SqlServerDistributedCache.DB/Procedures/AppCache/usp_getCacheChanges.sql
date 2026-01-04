CREATE PROCEDURE dbo.usp_getCacheChanges
(
	@LastTrackingNo BIGINT
)
AS
BEGIN
    DECLARE @TrackingNo BIGINT;

    SELECT @TrackingNo = MAX(ac.TrackingNo)
    FROM dbo.AppCache ac;

    SELECT
        ac.AppCacheKey
       ,ac.CacheValue
       ,@TrackingNo AS 'TrackingNo'
       ,ac.DataType AS 'DataType'
    FROM dbo.AppCache ac
    WHERE ac.TrackingNo > @LastTrackingNo;
END
