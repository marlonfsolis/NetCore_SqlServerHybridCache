--
-- Store the sessions values
-- DROP TABLE dbo.SessionCacheValue
--
CREATE TABLE dbo.SessionCacheValue
(
	SessionId VARCHAR(449) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    SessionKey VARCHAR(800) NOT NULL,
	SessionValue VARBINARY(MAX) NULL,
    TrackingNo BIGINT NOT NULL DEFAULT 0, 
    DataType VARCHAR(1000) NOT NULL

    CONSTRAINT PK_SessionCacheValue PRIMARY KEY NONCLUSTERED 
    (
	    SessionId ASC,
	    SessionKey ASC
    )
)
