namespace StarWin.Domain.Model.Entity.Military;

public sealed class MilitaryForceProfile
{
    public int EmpireId { get; set; }

    public IList<StarshipGroup> Fleet { get; } = new List<StarshipGroup>();

    public IList<MilitaryInstallationGroup> Bases { get; } = new List<MilitaryInstallationGroup>();

    public IList<GroundForceGroup> GroundForces { get; } = new List<GroundForceGroup>();

    public MilitaryPersonnelProfile Personnel { get; set; } = new();

    public NavyDoctrine NavyDoctrine { get; set; } = new();

    public string Notes { get; set; } = string.Empty;
}
