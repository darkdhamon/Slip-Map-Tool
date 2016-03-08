CREATE TABLE [dbo].[Ship]
(
   [Id] INT NOT NULL Identity(1,1) PRIMARY KEY, 
    [ShipName] NVARCHAR(100) NOT NULL, 
    [CurrentSystem] INT NOT NULL, 
    [PilotSkill] INT NULL , 
    CONSTRAINT [FK_Ship_StarSystem] FOREIGN KEY ([CurrentSystem]) REFERENCES [SlipMap].[StarSystem]([ID])
)