namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class EmpireReligion
{
    public int EmpireId { get; set; }

    public int ReligionId { get; set; }

    public string ReligionName { get; set; } = string.Empty;

    public decimal PopulationPercent { get; set; }
}
