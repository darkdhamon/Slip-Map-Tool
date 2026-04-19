namespace StarWin.Domain.Model.Entity.Military;

public sealed class NavyDoctrine
{
    public byte FighterEmphasisPercent { get; set; }

    public byte MissileEmphasisPercent { get; set; }

    public byte BeamWeaponEmphasisPercent { get; set; }

    public byte AssaultEmphasisPercent { get; set; }

    public byte DefenseEmphasisPercent { get; set; }
}
