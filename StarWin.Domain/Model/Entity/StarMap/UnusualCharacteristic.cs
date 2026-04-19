namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class UnusualCharacteristic
{
    public byte Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
