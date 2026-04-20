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

    public StarWinDatabaseWorkspace(IDbContextFactory<StarWinDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
        ReloadAsync().GetAwaiter().GetResult();
    }

    public IReadOnlyList<StarWinSector> Sectors { get; private set; } = [];

    public StarWinSector CurrentSector { get; private set; } = new() { Id = 0, Name = "No sectors loaded" };

    public IReadOnlyList<AlienRace> AlienRaces { get; private set; } = [];

    public IReadOnlyList<Empire> Empires { get; private set; } = [];

    public IReadOnlyList<EmpireContact> EmpireContacts { get; private set; } = [];

    public CivilizationGeneratorSettings CivilizationSettings { get; private set; } = BuildCivilizationSettings(new StarWinSector { Id = 0, Name = "No sectors loaded" });

    public ArmyGeneratorSettings ArmySettings { get; private set; } = new();

    public GurpsTemplate PreviewGurpsTemplate { get; private set; } = BuildPreviewGurpsTemplate();

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await reloadLock.WaitAsync(cancellationToken);
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var sectors = await dbContext.Sectors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(sector => sector.Configuration)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.AstralBodies)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.Worlds)
                    .ThenInclude(world => world.UnusualCharacteristics)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.Worlds)
                    .ThenInclude(world => world.Colony)
                        .ThenInclude(colony => colony!.Demographics)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.SpaceHabitats)
            .Include(sector => sector.History)
            .OrderBy(sector => sector.Name)
                .ToListAsync(cancellationToken);

            var alienRaces = await dbContext.AlienRaces
            .AsNoTracking()
            .OrderBy(race => race.Name)
                .ToListAsync(cancellationToken);

            var empires = await dbContext.Empires
            .AsNoTracking()
            .AsSplitQuery()
            .Include(empire => empire.RaceMemberships)
            .Include(empire => empire.Contacts)
            .OrderBy(empire => empire.Name)
                .ToListAsync(cancellationToken);

            Sectors = sectors;
            AlienRaces = alienRaces;
            Empires = empires;
            EmpireContacts = empires.SelectMany(empire => empire.Contacts).ToList();
            CurrentSector = sectors.FirstOrDefault() ?? new StarWinSector { Id = 0, Name = "No sectors loaded" };
            CivilizationSettings = BuildCivilizationSettings(CurrentSector);
            ArmySettings = new ArmyGeneratorSettings();
            PreviewGurpsTemplate = BuildPreviewGurpsTemplate();
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
