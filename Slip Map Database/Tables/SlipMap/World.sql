CREATE TABLE [dbo].[World]
(
   [Id] INT NOT NULL PRIMARY KEY, 
    [WorldType] INT NOT NULL, 
    [ParentWorld] INT NULL, 
    [StarSystem] INT NOT NULL, 
    [Name] NVARCHAR(50) NOT NULL, 
    [GMNotes] NVARCHAR(MAX) NULL, 
    [StarWinDetails] NVARCHAR(MAX) NULL
)
