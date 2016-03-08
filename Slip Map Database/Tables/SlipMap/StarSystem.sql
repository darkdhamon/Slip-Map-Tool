CREATE TABLE [SlipMap].[StarSystem]
(
   [Id] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(100) NULL Unique, 
    [StarWinId] INT NOT NULL, 
    [GMNotes] NVARCHAR(MAX) NULL, 
    [SystemNotes] NVARCHAR(MAX) NULL
)
