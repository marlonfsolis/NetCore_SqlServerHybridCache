CREATE PROCEDURE dbo.usp_getSessionChanges
(
	@Id VARCHAR(449),
	@LastTrackingNo BIGINT
)
AS
BEGIN
    DECLARE @TrackingNo BIGINT;

    SELECT @TrackingNo = MAX(scv.TrackingNo)
    FROM dbo.SessionCacheValue scv;

    SELECT
        SessionId = scv.SessionId
       ,SessionKey = scv.SessionKey
       ,SessionValue = scv.SessionValue
       ,TrackingNo = @TrackingNo
       ,DataType = scv.DataType
    FROM dbo.SessionCacheValue scv
    WHERE scv.SessionId = @Id
    AND scv.TrackingNo > @LastTrackingNo;
END
