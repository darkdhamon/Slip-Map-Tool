using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Model.ViewModel;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinDatabaseWorkspace : IStarWinWorkspace
{
    public StarWinDatabaseWorkspace(StarWinDbContext dbContext)
    {
        Sectors = dbContext.Sectors
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
            .ToList();

        AlienRaces = dbContext.AlienRaces
            .AsNoTracking()
            .OrderBy(race => race.Name)
            .ToList();

        Empires = dbContext.Empires
            .AsNoTracking()
            .AsSplitQuery()
            .Include(empire => empire.RaceMemberships)
            .Include(empire => empire.Contacts)
            .OrderBy(empire => empire.Name)
            .ToList();

        EmpireContacts = Empires.SelectMany(empire => empire.Contacts).ToList();
        CurrentSector = Sectors.FirstOrDefault() ?? new StarWinSector { Id = 0, Name = "No sectors loaded" };
        CivilizationSettings = BuildCivilizationSettings(CurrentSector);
        ArmySettings = new ArmyGeneratorSettings();
        PreviewGurpsTemplate = new GurpsTemplate
        {
            Name = "GURPS Fourth Edition Draft",
            Edition = GurpsTemplateEdition.FourthEdition,
            Notes = "Select an alien race to build a full template."
        };
    }

    public IReadOnlyList<StarWinSector> Sectors { get; }

    public StarWinSector CurrentSector { get; }

    public IReadOnlyList<AlienRace> AlienRaces { get; }

    public IReadOnlyList<Empire> Empires { get; }

    public IReadOnlyList<EmpireContact> EmpireContacts { get; }

    public CivilizationGeneratorSettings CivilizationSettings { get; }

    public ArmyGeneratorSettings ArmySettings { get; }

    public GurpsTemplate PreviewGurpsTemplate { get; }

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
}
