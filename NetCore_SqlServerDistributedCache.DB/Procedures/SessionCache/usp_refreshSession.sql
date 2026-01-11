CREATE PROCEDURE dbo.usp_refreshSession
(
    @Id VARCHAR(449),
    @AbsoluteExpiration DATETIME2(7),
    @UtcNow DATETIME2(7)
)
AS
BEGIN
    UPDATE sc
    SET sc.AbsoluteExpiration = @AbsoluteExpiration
    FROM dbo.SessionCache sc
    WHERE sc.SessionId = @Id
    AND sc.AbsoluteExpiration >= @UtcNow;
END