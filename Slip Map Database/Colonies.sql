﻿CREATE TABLE [dbo].[Colonies]
(
  [Id] INT NOT NULL PRIMARY KEY, 
    [Name] VARCHAR(50) NOT NULL DEFAULT '', 
    [GMNotes] VARCHAR(8000) NOT NULL DEFAULT '', 
    [MajorImport] VARCHAR(50) NOT NULL DEFAULT '', 
    [MajorExport] VARCHAR(50) NOT NULL DEFAULT '', 
    [Stability] INT NOT NULL DEFAULT 10, 
    [Law] INT NOT NULL DEFAULT 1 , 
    [Crime] INT NOT NULL DEFAULT 1
)
