namespace StarWin.Domain.Model.ViewModel;

public sealed class CivilizationGeneratorSettings
{
    public string SectorPath { get; set; } = string.Empty;

    public bool EnableLowTechnologyPass { get; set; } = true;

    public bool UseQuickMethod { get; set; }

    public bool EnableRandomEvents { get; set; } = true;

    public bool CompactEmpires { get; set; } = true;

    public bool LiberateAlliedWorlds { get; set; } = true;

    public decimal VictoryValue { get; set; } = 1.75m;

    public int MilitaryBonus { get; set; } = 17500;

    public int CenturyNumber { get; set; }

    public int RandomEventFactor { get; set; } = 5;

    public CivilizationGenerationLogOptions LogOptions { get; } = new();

    public IList<TechnologyLevelExpansionRange> TechnologyLevelExpansionRanges { get; } =
        new List<TechnologyLevelExpansionRange>
        {
            new(6, 1),
            new(7, 2),
            new(8, 3),
            new(9, 5),
            new(10, 4),
        };
}
