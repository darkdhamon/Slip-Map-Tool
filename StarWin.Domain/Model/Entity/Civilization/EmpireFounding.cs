namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class EmpireFounding
{
    public EmpireOrigin Origin { get; set; } = EmpireOrigin.Unknown;

    public int? FoundingWorldId { get; set; }

    public int? FoundingColonyId { get; set; }

    public int? ParentEmpireId { get; set; }

    public int? FoundingRaceId { get; set; }

    public int? FoundedCentury { get; set; }
}
