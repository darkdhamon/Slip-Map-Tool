CREATE TABLE [dbo].[Colony]
(
   [Id] INT NOT NULL PRIMARY KEY identity(1,1), 
    [Name] NVARCHAR(50) NOT NULL, 
    [Crime] INT NOT NULL, 
    [Law] INT NOT NULL, 
    [Stability] INT NOT NULL, 
    [GMNotes] NVARCHAR(MAX) NOT NULL, 
    [MajorImport] INT NOT NULL, 
    [MajorExport] INT NOT NULL, 
    [World] INT NOT NULL
)
