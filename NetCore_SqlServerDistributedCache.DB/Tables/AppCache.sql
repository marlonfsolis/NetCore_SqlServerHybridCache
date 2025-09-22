CREATE TABLE [dbo].[AppCache]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Value] VARBINARY(MAX) NULL, 
    [AbsoluteExpiration] DATETIMEOFFSET NULL
)
