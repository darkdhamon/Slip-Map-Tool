namespace StarWin.Domain.Model.Entity.StarMap;

public abstract class OrbitalBody
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public OrbitTargetKind OrbitTargetKind { get; set; }

    public int OrbitTargetId { get; set; }

    public int? BuiltByEmpireId { get; set; }

    public int? ControlledByEmpireId { get; set; }

    public int? ConstructedCentury { get; set; }

    public double? OrbitRadiusKm { get; set; }
}
