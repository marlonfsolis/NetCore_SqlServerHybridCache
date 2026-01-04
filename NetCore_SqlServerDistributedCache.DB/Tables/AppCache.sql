--
-- Store the data cached accorss the app
-- DROP TABLE dbo.AppCache
--
CREATE TABLE dbo.AppCache
(
	AppCacheKey VARCHAR(900) NOT NULL PRIMARY KEY, 
    CacheValue VARBINARY(MAX) NULL, 
    AbsoluteExpiration DATETIMEOFFSET NOT NULL, 
    TrackingNo BIGINT NOT NULL DEFAULT 0, 
    DataType VARCHAR(1000) NOT NULL
)
