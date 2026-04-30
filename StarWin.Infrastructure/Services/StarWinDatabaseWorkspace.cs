using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Model.ViewModel;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinDatabaseWorkspace : IStarWinWorkspace
{
    private readonly IDbContextFactory<StarWinDbContext> dbContextFactory;
    private readonly SemaphoreSlim reloadLock = new(1, 1);
    private static readonly StarWinSector EmptySector = new() { Id = 0, Name = "No sectors loaded" };

    public StarWinDatabaseWorkspace(IDbContextFactory<StarWinDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public bool IsLoaded { get; private set; }

    public IReadOnlyList<StarWinSector> Sectors { get; private set; } = [];

    public StarWinSector CurrentSector { get; private set; } = EmptySector;

    public IReadOnlyList<AlienRace> AlienRaces { get; private set; } = [];

    public IReadOnlyList<Empire> Empires { get; private set; } = [];

    public CivilizationGeneratorSettings CivilizationSettings { get; private set; } = BuildCivilizationSettings(EmptySector);

    public ArmyGeneratorSettings ArmySettings { get; private set; } = new();

    public GurpsTemplate PreviewGurpsTemplate { get; private set; } = BuildPreviewGurpsTemplate();

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await reloadLock.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var explorerContext = await StarWinExplorerContextLoader.LoadAsync(dbContext, cancellationToken);

            Sectors = explorerContext.Sectors;
            AlienRaces = explorerContext.AlienRaces;
            Empires = explorerContext.Empires;
            CurrentSector = explorerContext.CurrentSector;
            CivilizationSettings = BuildCivilizationSettings(CurrentSector);
            ArmySettings = new ArmyGeneratorSettings();
            PreviewGurpsTemplate = BuildPreviewGurpsTemplate();
            IsLoaded = true;
        }
        finally
        {
            reloadLock.Release();
        }
    }

    private static CivilizationGeneratorSettings BuildCivilizationSettings(StarWinSector sector)
    {
        return new CivilizationGeneratorSettings
        {
            SectorPath = $"data/{sector.Name}",
            EnableLowTechnologyPass = true,
            EnableRandomEvents = true,
            VictoryValue = 1.75m,
            MilitaryBonus = 17_500,
            CenturyNumber = 105,
            RandomEventFactor = 5,
            CompactEmpires = true,
            LiberateAlliedWorlds = true
        };
    }

    private static GurpsTemplate BuildPreviewGurpsTemplate()
    {
        return new GurpsTemplate
        {
            Name = "GURPS Fourth Edition Draft",
            Edition = GurpsTemplateEdition.FourthEdition,
            Notes = "Select an alien race to build a full template."
        };
    }
}
