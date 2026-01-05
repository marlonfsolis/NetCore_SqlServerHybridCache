CREATE PROCEDURE dbo.usp_refreshSession
(
    @Id VARCHAR(449),
    @AbsoluteExpiration DATETIMEOFFSET,
    @UtcNow DATETIMEOFFSET
)
AS
BEGIN
    UPDATE sc
    SET sc.AbsoluteExpiration = @AbsoluteExpiration
    FROM dbo.SessionCache sc
    WHERE sc.SessionId = @Id
    AND sc.AbsoluteExpiration >= @UtcNow;
END