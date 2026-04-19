namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class Star : AstralBody
{
    public int Sequence { get; set; }

    public string SpectralClass
    {
        get => Classification;
        set => Classification = value;
    }
}
