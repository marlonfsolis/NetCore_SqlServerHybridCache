CREATE PROCEDURE dbo.usp_clearSession
(
    @Id VARCHAR(449)
)
AS
BEGIN
    UPDATE SessionCacheValue
    SET SessionValue = NULL
        ,TrackingNo = TrackingNo + 1
    WHERE SessionId = @Id;
END
