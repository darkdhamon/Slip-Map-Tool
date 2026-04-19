namespace StarWin.Domain.Model.Entity.Military;

public sealed class MilitaryPersonnelProfile
{
    public byte CrewRating { get; set; }

    public string CrewQuality { get; set; } = string.Empty;

    public byte TroopRating { get; set; }

    public string TroopQuality { get; set; } = string.Empty;

    public ConscriptionPolicy ConscriptionPolicy { get; set; } = ConscriptionPolicy.Unknown;
}
