namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class AlienBiologyProfile
{
    public byte PsiPower { get; set; }

    public PsiPowerRating PsiRating { get; set; }

    public byte Body { get; set; }

    public byte Mind { get; set; }

    public byte Speed { get; set; }

    public byte Lifespan { get; set; }
}
