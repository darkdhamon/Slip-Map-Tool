namespace StarWin.Domain.Model.Entity.Military;

public sealed class StarshipGroup
{
    public StarshipClass Class { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
