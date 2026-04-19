
namespace StarWin.Domain.Model.ViewModel;

public sealed class AlienRaceExportProfile
{
    public required AlienRace Race { get; init; }

    public Empire? Empire { get; init; }

    public World? HomeWorld { get; init; }

    public IList<Colony> Colonies { get; } = new List<Colony>();

    public IList<EmpireContact> Contacts { get; } = new List<EmpireContact>();
}
