namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class Religion
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsUserDefined { get; set; }
}
