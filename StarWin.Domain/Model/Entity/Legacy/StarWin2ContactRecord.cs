namespace StarWin.Domain.Model.Entity.Legacy;

public sealed class StarWin2ContactRecord
{
    public ushort Empire1Id { get; init; }

    public ushort Empire2Id { get; init; }

    public byte Relation { get; init; }

    public byte Age { get; init; }
}
