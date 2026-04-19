
namespace StarWin.Domain.Model.ViewModel;

public sealed class CivilizationGenerationResult
{
    public required StarWinSector Sector { get; init; }

    public int StartCentury { get; init; }

    public int EndCentury { get; init; }

    public IList<Empire> Empires { get; } = new List<Empire>();

    public IList<Colony> Colonies { get; } = new List<Colony>();

    public IList<SpaceHabitat> SpaceHabitats { get; } = new List<SpaceHabitat>();

    public IList<HistoryEvent> Events { get; } = new List<HistoryEvent>();
}
