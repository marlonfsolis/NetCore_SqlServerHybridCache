--
-- Store the sessions
-- DROP TABLE dbo.SessionCache
--
CREATE TABLE dbo.SessionCache
(
	SessionId VARCHAR(449) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    AbsoluteExpiration DATETIME2(7) NOT NULL

    CONSTRAINT PK_SessionCache PRIMARY KEY NONCLUSTERED 
    (
	    SessionId ASC
    )
)
