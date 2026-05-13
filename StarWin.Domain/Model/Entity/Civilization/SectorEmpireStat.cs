namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class SectorEmpireStat
{
    public int SectorId { get; set; }

    public int EmpireId { get; set; }

    public int ControlledWorldCount { get; set; }

    public int TrackedWorldCount { get; set; }

    public DateTime LastCalculatedAtUtc { get; set; } = DateTime.UtcNow;
}
