CREATE PROCEDURE dbo.usp_getSessionChanges
(
	@Id NVARCHAR(100),
	@LastTrackingNo BIGINT
)
AS
BEGIN
    DECLARE @TrackingNo BIGINT;

    SELECT @TrackingNo = MAX(sc.TrackingNo)
    FROM dbo.SessionCache sc;

    SELECT
        sc.SessionId
       ,sc.SessionKey
       ,sc.SessionValue
       ,TrackingNo = @TrackingNo
       ,DataType = sc.DataType
    FROM dbo.SessionCache sc
    WHERE sc.TrackingNo > @LastTrackingNo;
END
