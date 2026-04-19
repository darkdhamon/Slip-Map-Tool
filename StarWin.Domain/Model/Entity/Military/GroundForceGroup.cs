namespace StarWin.Domain.Model.Entity.Military;

public sealed class GroundForceGroup
{
    public GroundForceType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
