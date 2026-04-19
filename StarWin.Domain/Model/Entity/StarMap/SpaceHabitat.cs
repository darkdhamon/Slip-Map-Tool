namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class SpaceHabitat : OrbitalBody
{
    public long Population { get; set; }

    public Colony? Colony { get; set; }

    public IList<string> Facilities { get; } = new List<string>();
}
