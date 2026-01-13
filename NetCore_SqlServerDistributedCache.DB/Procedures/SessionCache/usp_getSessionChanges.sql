CREATE PROCEDURE dbo.usp_getSessionChanges
(
	@Id VARCHAR(449),
	@TrackingListJson NVARCHAR(MAX)
)
AS
BEGIN
    ---- Test Data
    --DECLARE @Id VARCHAR(449) = '99ff04ac-da9f-447b-9966-82e7984ada5f',
	   --     @TrackingListJson NVARCHAR(MAX) = N'[{"Key":"q1","TrackingNo":8},{"Key":"q111","TrackingNo":3}]';


    DROP TABLE IF EXISTS #TrackingList;
    SELECT SessionKey
		  ,TrackingNo
    INTO #TrackingList
    FROM OPENJSON(@TrackingListJson) WITH(
        SessionKey VARCHAR(800) '$.Key',
        TrackingNo BIGINT '$.TrackingNo'
    )

    IF NOT EXISTS (
        SELECT 1 FROM #TrackingList tl
    ) BEGIN
        SELECT
            SessionId = scv.SessionId
           ,SessionKey = scv.SessionKey
           ,SessionValue = scv.SessionValue
           ,TrackingNo = scv.TrackingNo
           ,DataType = scv.DataType
        FROM dbo.SessionCacheValue scv
        WHERE scv.SessionId = @Id;
    END
    ELSE BEGIN
        SELECT
            SessionId = scv.SessionId
           ,SessionKey = scv.SessionKey
           ,SessionValue = scv.SessionValue
           ,TrackingNo = scv.TrackingNo
           ,DataType = scv.DataType
        FROM dbo.SessionCacheValue scv
        INNER JOIN #TrackingList tl ON (tl.SessionKey = scv.SessionKey)
        WHERE scv.SessionId = @Id
        AND scv.TrackingNo > tl.TrackingNo;
    END
END
