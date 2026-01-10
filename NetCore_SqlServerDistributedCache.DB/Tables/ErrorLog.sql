--
-- Store the errors accorss the app
-- DROP TABLE dbo.ErrorLog
--
CREATE TABLE dbo.ErrorLog
(
	ErrorLogId BIGINT NOT NULL IDENTITY PRIMARY KEY, 
    ErrorMessage NVARCHAR(MAX) NULL,
    ErrorDetail NVARCHAR(MAX) NULL,
    ErrorDate DATETIME2(7) NOT NULL DEFAULT (SYSDATETIME())
)