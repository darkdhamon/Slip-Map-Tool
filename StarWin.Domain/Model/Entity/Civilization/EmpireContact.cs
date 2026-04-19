namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class EmpireContact
{
    public int EmpireId { get; set; }

    public int OtherEmpireId { get; set; }

    public string Relation { get; set; } = string.Empty;

    public byte RelationCode { get; set; }

    public byte Age { get; set; }
}
