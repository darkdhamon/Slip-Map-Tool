namespace StarWin.Domain.Model.ViewModel;

public sealed class ArmyGeneratorSettings
{
    public int Credits { get; set; } = 100;

    public byte Militancy { get; set; } = 10;

    public byte TechnologyLevel { get; set; } = 8;

    public int Colonies { get; set; } = 1;
}
