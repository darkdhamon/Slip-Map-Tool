namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class EmpireRaceMembership
{
    public int EmpireId { get; set; }

    public int RaceId { get; set; }

    public EmpireRaceRole Role { get; set; } = EmpireRaceRole.Member;

    public long PopulationMillions { get; set; }

    public bool IsPrimary { get; set; }
}
