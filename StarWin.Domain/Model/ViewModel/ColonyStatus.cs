
namespace StarWin.Domain.Model.ViewModel;

public sealed class ColonyStatus
{
    public required Colony Colony { get; init; }

    public required World World { get; init; }

    public string WorldDisplay { get; set; } = string.Empty;

    public string PopulationDisplay { get; set; } = string.Empty;

    public string LawDisplay { get; set; } = string.Empty;

    public string StabilityDisplay { get; set; } = string.Empty;

    public string CrimeDisplay { get; set; } = string.Empty;

    public string ColonyAgeDisplay { get; set; } = string.Empty;

    public IList<string> Facilities { get; } = new List<string>();
}
