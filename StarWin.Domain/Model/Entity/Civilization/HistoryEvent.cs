namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class HistoryEvent
{
    public int? SectorId { get; set; }

    public int Century { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int? RaceId { get; set; }

    public int? OtherRaceId { get; set; }

    public int? EmpireId { get; set; }

    public int? ColonyId { get; set; }

    public int? PlanetId { get; set; }

    public int? StarSystemId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? ImportDataJson { get; set; }
}
