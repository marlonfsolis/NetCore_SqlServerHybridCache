CREATE TABLE dbo.SessionCache
(
	SessionId NVARCHAR(449) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    SessionKey VARCHAR(1000) NOT NULL,
	SessionValue VARBINARY(MAX) NULL, 
    AbsoluteExpiration DATETIMEOFFSET NOT NULL, 
    TrackingNo BIGINT NOT NULL DEFAULT 0, 
    DataType VARCHAR(1000) NOT NULL

    CONSTRAINT PK_SessionCache PRIMARY KEY NONCLUSTERED 
    (
	    SessionId ASC,
	    SessionKey ASC
    ),
)
