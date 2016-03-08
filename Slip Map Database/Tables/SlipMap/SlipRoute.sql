CREATE TABLE [SlipMap].[SlipRoute]
(
    [Origin] INT NOT NULL, 
    [Destination] INT NOT NULL,
    CONSTRAINT [PK_SlipRoute] PRIMARY KEY ([origin],[destination]), 
    CONSTRAINT [CK_SlipRoute_Origin_Destination] CHECK ([origin] <> [Destination]), 
    CONSTRAINT [FK_SlipRoute_Origin] FOREIGN KEY ([Origin]) REFERENCES [SlipMap].[StarSystem]([ID]),
    CONSTRAINT [FK_SlipRoute_Destination] FOREIGN KEY ([Destination]) REFERENCES [SlipMap].[StarSystem]([ID])
)
GO

CREATE TRIGGER [SlipMap].[Trigger_SlipRoute]
    ON [SlipMap].[SlipRoute]
    FOR INSERT
    AS
    BEGIN
        SET NoCount ON
        if not exists (
            select count(*) 
               from SlipRoute 
               where 
                  inserted.Origin = Destination and
                  inserted.Destination = Origin
        )
            insert into SlipRoute (Origin, Destination)
            values (inserted.Destination, inserted.Origin)
    END