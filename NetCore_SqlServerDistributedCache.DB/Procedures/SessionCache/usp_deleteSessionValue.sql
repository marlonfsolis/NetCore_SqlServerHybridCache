CREATE PROCEDURE dbo.usp_deleteSessionValue
(
	@Id VARCHAR(449),
	@Key VARCHAR(800)
)
AS
BEGIN
	UPDATE dbo.SessionCacheValue
	SET SessionValue = NULL
	   ,TrackingNo = TrackingNo + 1
	WHERE SessionId = @Id
	AND SessionKey = @Key;
END