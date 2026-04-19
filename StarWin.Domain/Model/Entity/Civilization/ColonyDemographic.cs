namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class ColonyDemographic
{
    public int ColonyId { get; set; }

    public int RaceId { get; set; }

    public string RaceName { get; set; } = string.Empty;

    public decimal PopulationPercent { get; set; }

    public long Population { get; set; }
}
