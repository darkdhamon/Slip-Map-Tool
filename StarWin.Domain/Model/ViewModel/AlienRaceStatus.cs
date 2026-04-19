
namespace StarWin.Domain.Model.ViewModel;

public sealed class AlienRaceStatus
{
    public required AlienRace AlienRace { get; init; }

    public Empire? CurrentEmpire { get; init; }

    public string PsiPowerRating { get; set; } = string.Empty;

    public string FaithDisplay { get; set; } = string.Empty;

    public IList<string> LimbDisplay { get; } = new List<string>();

    public IList<string> AbilityDisplay { get; } = new List<string>();

    public IList<string> BodyCharacteristicDisplay { get; } = new List<string>();

    public IList<string> BodyColorDisplay { get; } = new List<string>();

    public IList<string> HairColorDisplay { get; } = new List<string>();

    public IList<string> EyeCharacteristicDisplay { get; } = new List<string>();

    public IList<string> EyeColorDisplay { get; } = new List<string>();
}
