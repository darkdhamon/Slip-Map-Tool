using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Military;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Infrastructure.Data;

public sealed class StarWinDbContext(DbContextOptions<StarWinDbContext> options) : DbContext(options)
{
    public DbSet<StarWinSector> Sectors => Set<StarWinSector>();

    public DbSet<StarSystem> StarSystems => Set<StarSystem>();

    public DbSet<World> Worlds => Set<World>();

    public DbSet<SpaceHabitat> SpaceHabitats => Set<SpaceHabitat>();

    public DbSet<AlienRace> AlienRaces => Set<AlienRace>();

    public DbSet<Empire> Empires => Set<Empire>();

    public DbSet<Religion> Religions => Set<Religion>();

    public DbSet<Colony> Colonies => Set<Colony>();

    public DbSet<HistoryEvent> HistoryEvents => Set<HistoryEvent>();

    public DbSet<EntityImage> EntityImages => Set<EntityImage>();

    public DbSet<EntityNote> EntityNotes => Set<EntityNote>();

    public DbSet<SectorSavedRoute> SectorSavedRoutes => Set<SectorSavedRoute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureStarMap(modelBuilder);
        ConfigureCivilization(modelBuilder);
        ConfigureMedia(modelBuilder);
        ConfigureNotes(modelBuilder);
    }

    private static void ConfigureStarMap(ModelBuilder modelBuilder)
    {
        var coordinatesConverter = new ValueConverter<Coordinates, string>(
            coordinates => $"{coordinates.X},{coordinates.Y},{coordinates.Z}",
            value => ParseCoordinates(value));

        modelBuilder.Entity<StarWinSector>(entity =>
        {
            entity.ToTable("Sectors");
            entity.HasKey(sector => sector.Id);
            entity.Property(sector => sector.Id).ValueGeneratedNever();
            entity.Property(sector => sector.Name).HasMaxLength(160);
            entity.HasIndex(sector => sector.Name).IsUnique();
            entity.HasOne(sector => sector.Configuration)
                .WithOne()
                .HasForeignKey<SectorConfiguration>(configuration => configuration.SectorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(sector => sector.Systems).WithOne().HasForeignKey(system => system.SectorId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(sector => sector.SavedRoutes).WithOne().HasForeignKey(route => route.SectorId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(sector => sector.History).WithOne().HasForeignKey(historyEvent => historyEvent.SectorId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SectorConfiguration>(entity =>
        {
            entity.ToTable("SectorConfigurations");
            entity.HasKey(configuration => configuration.SectorId);
            entity.Property(configuration => configuration.OffLaneMaximumDistanceParsecs).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl9AndBelowMaximumConnectionsPerSystem);
            entity.Property(configuration => configuration.AdditionalCrossEmpireConnectionsPerSystem);
            entity.Property(configuration => configuration.Tl6HyperlaneName).HasMaxLength(80);
            entity.Property(configuration => configuration.Tl6MaximumDistanceParsecs).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl6OffLaneSpeedMultiplier).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl6HyperlaneSpeedModifier).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl7HyperlaneName).HasMaxLength(80);
            entity.Property(configuration => configuration.Tl7MaximumDistanceParsecs).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl7OffLaneSpeedMultiplier).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl7HyperlaneSpeedModifier).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl8HyperlaneName).HasMaxLength(80);
            entity.Property(configuration => configuration.Tl8MaximumDistanceParsecs).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl8OffLaneSpeedMultiplier).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl8HyperlaneSpeedModifier).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl9HyperlaneName).HasMaxLength(80);
            entity.Property(configuration => configuration.Tl9MaximumDistanceParsecs).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl9OffLaneSpeedMultiplier).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl9HyperlaneSpeedModifier).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl10HyperlaneName).HasMaxLength(80);
            entity.Property(configuration => configuration.Tl10MaximumDistanceParsecs).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl10OffLaneSpeedMultiplier).HasPrecision(8, 3);
            entity.Property(configuration => configuration.Tl10HyperlaneSpeedModifier).HasPrecision(8, 3);
        });

        modelBuilder.Entity<SectorSavedRoute>(entity =>
        {
            entity.ToTable("SectorSavedRoutes");
            entity.HasKey(route => route.Id);
            entity.HasIndex(route => new { route.SectorId, route.SourceSystemId, route.TargetSystemId }).IsUnique();
            entity.Property(route => route.DistanceParsecs).HasPrecision(8, 3);
            entity.Property(route => route.TravelTimeYears).HasPrecision(12, 6);
            entity.Property(route => route.TierName).HasMaxLength(80);
            entity.Property(route => route.PrimaryOwnerEmpireName).HasMaxLength(160);
            entity.Property(route => route.SecondaryOwnerEmpireName).HasMaxLength(160);
            entity.Property(route => route.IsUserPersisted);
        });

        modelBuilder.Entity<StarSystem>(entity =>
        {
            entity.ToTable("StarSystems");
            entity.HasKey(system => system.Id);
            entity.Property(system => system.Id).ValueGeneratedNever();
            entity.HasIndex(system => new { system.SectorId, system.Name }).IsUnique();
            entity.HasIndex(system => new { system.SectorId, system.LegacySystemId });
            entity.Property(system => system.Name).HasMaxLength(160);
            entity.Property(system => system.Coordinates).HasConversion(coordinatesConverter).HasMaxLength(48);
            entity.Ignore(system => system.Stars);
            entity.Ignore(system => system.Planets);
            entity.Ignore(system => system.Moons);
            entity.HasMany(system => system.AstralBodies).WithOne().OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(system => system.Worlds).WithOne().OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(system => system.SpaceHabitats).WithOne().OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AstralBody>(entity =>
        {
            entity.ToTable("AstralBodies");
            entity.HasKey(body => body.Id);
            entity.Property(body => body.Kind).HasConversion<string>().HasMaxLength(40);
            entity.Property(body => body.Role).HasConversion<string>().HasMaxLength(40);
            entity.Property(body => body.Classification).HasMaxLength(80);
            entity.Ignore(body => body.PlanetOrbitTenthsAu);
        });

        modelBuilder.Entity<World>(entity =>
        {
            entity.ToTable("Worlds");
            entity.HasKey(world => world.Id);
            entity.Property(world => world.Id).ValueGeneratedNever();
            entity.Property(world => world.Kind).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(world => new { world.StarSystemId, world.LegacyPlanetId });
            entity.HasIndex(world => new { world.StarSystemId, world.LegacyMoonId });
            entity.Property(world => world.Name).HasMaxLength(160);
            entity.Property(world => world.WorldType).HasMaxLength(80);
            entity.Property(world => world.AtmosphereType).HasMaxLength(80);
            entity.Property(world => world.AtmosphereComposition).HasMaxLength(160);
            entity.Property(world => world.WaterType).HasMaxLength(80);
            entity.OwnsOne(world => world.Hydrography);
            entity.OwnsOne(world => world.MineralResources);
            entity.HasMany(world => world.UnusualCharacteristics).WithOne().OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(world => world.Colony).WithOne().HasForeignKey<Colony>(colony => colony.WorldId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UnusualCharacteristic>(entity =>
        {
            entity.ToTable("UnusualCharacteristics");
            entity.Property<int>("Id");
            entity.HasKey("Id");
            entity.Property(characteristic => characteristic.Name).HasMaxLength(160);
            entity.Property(characteristic => characteristic.Notes).HasMaxLength(600);
        });

        modelBuilder.Entity<SpaceHabitat>(entity =>
        {
            entity.ToTable("SpaceHabitats");
            entity.HasKey(habitat => habitat.Id);
            entity.Property(habitat => habitat.Id).ValueGeneratedNever();
            entity.Property(habitat => habitat.OrbitTargetKind).HasConversion<string>().HasMaxLength(40);
            entity.Property(habitat => habitat.Name).HasMaxLength(160);
            entity.Ignore(habitat => habitat.Facilities);
            entity.Ignore(habitat => habitat.Colony);
        });
    }

    private static void ConfigureCivilization(ModelBuilder modelBuilder)
    {
        var stringListConverter = new ValueConverter<IList<string>, string>(
            values => System.Text.Json.JsonSerializer.Serialize(values, (System.Text.Json.JsonSerializerOptions?)null),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>());
        modelBuilder.Entity<AlienRace>(entity =>
        {
            entity.ToTable("AlienRaces");
            entity.HasKey(race => race.Id);
            entity.Property(race => race.Id).ValueGeneratedNever();
            entity.Property(race => race.Name).HasMaxLength(160);
            entity.Property(race => race.EnvironmentType).HasMaxLength(80);
            entity.Property(race => race.BodyChemistry).HasMaxLength(80);
            entity.Property(race => race.BodyCoverType).HasMaxLength(120);
            entity.Property(race => race.AppearanceType).HasMaxLength(120);
            entity.Property(race => race.Diet).HasMaxLength(80);
            entity.Property(race => race.Reproduction).HasMaxLength(80);
            entity.Property(race => race.ReproductionMethod).HasMaxLength(120);
            entity.Property(race => race.DevotionLevel).HasConversion<string>().HasMaxLength(60);
            entity.Property(race => race.GravityPreference).HasMaxLength(80);
            entity.Property(race => race.TemperaturePreference).HasMaxLength(80);
            entity.Property(race => race.AtmosphereBreathed).HasMaxLength(120);
            entity.Property(race => race.HairType).HasMaxLength(120);
            entity.Property(race => race.ColorPattern).HasMaxLength(120);
            entity.Property(race => race.ImportDataJson);
            entity.OwnsOne(race => race.CivilizationProfile, owned =>
            {
                owned.Ignore(profile => profile.TechLevel);
                owned.Ignore(profile => profile.SpatialAge);
            });
            entity.OwnsOne(race => race.BiologyProfile, owned =>
            {
                owned.Property(profile => profile.PsiRating).HasConversion<string>().HasMaxLength(60);
            });
            entity.Property(race => race.LimbTypes).HasConversion(stringListConverter);
            entity.Property(race => race.Abilities).HasConversion(stringListConverter);
            entity.Property(race => race.BodyCharacteristics).HasConversion(stringListConverter);
            entity.Property(race => race.EyeCharacteristics).HasConversion(stringListConverter);
            entity.Property(race => race.EyeColors).HasConversion(stringListConverter);
            entity.Property(race => race.HairColors).HasConversion(stringListConverter);
            entity.Property(race => race.Colors).HasConversion(stringListConverter);
        });

        modelBuilder.Entity<Empire>(entity =>
        {
            entity.ToTable("Empires");
            entity.HasKey(empire => empire.Id);
            entity.Property(empire => empire.Id).ValueGeneratedNever();
            entity.Property(empire => empire.Name).HasMaxLength(160);
            entity.Property(empire => empire.GovernmentType).HasMaxLength(120);
            entity.Property(empire => empire.ImportDataJson);
            entity.Property(empire => empire.ExpansionPolicy).HasConversion<string>().HasMaxLength(80);
            entity.OwnsOne(empire => empire.CivilizationProfile);
            entity.OwnsOne(empire => empire.CivilizationModifiers);
            entity.OwnsOne(empire => empire.Founding, owned =>
            {
                owned.Property(founding => founding.Origin).HasConversion<string>().HasMaxLength(80);
            });
            entity.OwnsOne(empire => empire.MilitaryForces, owned =>
            {
                owned.OwnsMany(profile => profile.Fleet, fleet =>
                {
                    fleet.ToTable("EmpireFleet");
                    fleet.Property<int>("Id");
                    fleet.HasKey("Id");
                    fleet.Property(ship => ship.Class).HasConversion<string>().HasMaxLength(80);
                    fleet.Property(ship => ship.Name).HasMaxLength(120);
                    fleet.Property(ship => ship.Code).HasMaxLength(16);
                });
                owned.OwnsMany(profile => profile.Bases, bases =>
                {
                    bases.ToTable("EmpireMilitaryBases");
                    bases.Property<int>("Id");
                    bases.HasKey("Id");
                    bases.Property(militaryBase => militaryBase.Type).HasConversion<string>().HasMaxLength(80);
                    bases.Property(militaryBase => militaryBase.Name).HasMaxLength(120);
                    bases.Property(militaryBase => militaryBase.Code).HasMaxLength(16);
                });
                owned.OwnsMany(profile => profile.GroundForces, forces =>
                {
                    forces.ToTable("EmpireGroundForces");
                    forces.Property<int>("Id");
                    forces.HasKey("Id");
                    forces.Property(force => force.Type).HasConversion<string>().HasMaxLength(80);
                    forces.Property(force => force.Name).HasMaxLength(120);
                });
                owned.OwnsOne(profile => profile.Personnel, personnel =>
                {
                    personnel.Property(profile => profile.ConscriptionPolicy).HasConversion<string>().HasMaxLength(80);
                    personnel.Property(profile => profile.CrewQuality).HasMaxLength(80);
                    personnel.Property(profile => profile.TroopQuality).HasMaxLength(80);
                });
                owned.OwnsOne(profile => profile.NavyDoctrine);
                owned.Property(profile => profile.Notes).HasMaxLength(1_200);
            });
            entity.HasMany(empire => empire.RaceMemberships).WithOne().OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(empire => empire.Contacts).WithOne().OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(empire => empire.Religions).WithOne().HasForeignKey(religion => religion.EmpireId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmpireRaceMembership>(entity =>
        {
            entity.ToTable("EmpireRaceMemberships");
            entity.Property<int>("Id");
            entity.HasKey("Id");
            entity.Property(membership => membership.Role).HasConversion<string>().HasMaxLength(80);
        });

        modelBuilder.Entity<EmpireContact>(entity =>
        {
            entity.ToTable("EmpireContacts");
            entity.Property<int>("Id");
            entity.HasKey("Id");
            entity.Property(contact => contact.Relation).HasMaxLength(160);
        });

        modelBuilder.Entity<Religion>(entity =>
        {
            entity.ToTable("Religions");
            entity.HasKey(religion => religion.Id);
            entity.Property(religion => religion.Name).HasMaxLength(160);
            entity.Property(religion => religion.Type).HasMaxLength(120);
            entity.HasIndex(religion => religion.Name).IsUnique();
        });

        modelBuilder.Entity<EmpireReligion>(entity =>
        {
            entity.ToTable("EmpireReligions");
            entity.Property(religion => religion.ReligionName).HasMaxLength(160);
            entity.HasKey(religion => new { religion.EmpireId, religion.ReligionId });
            entity.HasOne<Religion>().WithMany().HasForeignKey(religion => religion.ReligionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Colony>(entity =>
        {
            entity.ToTable("Colonies");
            entity.HasKey(colony => colony.Id);
            entity.Property(colony => colony.Id).ValueGeneratedNever();
            entity.Property(colony => colony.Name).HasMaxLength(160);
            entity.Property(colony => colony.WorldKind).HasConversion<string>().HasMaxLength(40);
            entity.Property(colony => colony.ColonistRaceName).HasMaxLength(160);
            entity.Property(colony => colony.AllegianceName).HasMaxLength(160);
            entity.Property(colony => colony.PoliticalStatus).HasConversion<string>().HasMaxLength(80);
            entity.Property(colony => colony.ColonyClass).HasMaxLength(80);
            entity.Property(colony => colony.Starport).HasMaxLength(80);
            entity.Property(colony => colony.GovernmentType).HasMaxLength(120);
            entity.Property(colony => colony.ExportResource).HasMaxLength(120);
            entity.Property(colony => colony.ImportResource).HasMaxLength(120);
            entity.HasMany(colony => colony.Demographics).WithOne().HasForeignKey(demographic => demographic.ColonyId).OnDelete(DeleteBehavior.Cascade);
            entity.Ignore(colony => colony.Facilities);
            entity.Ignore(colony => colony.HasLegacySpaceHabitatFacility);
        });

        modelBuilder.Entity<ColonyDemographic>(entity =>
        {
            entity.ToTable("ColonyDemographics");
            entity.Property<int>("Id");
            entity.HasKey("Id");
            entity.Property(demographic => demographic.RaceName).HasMaxLength(160);
            entity.Property(demographic => demographic.PopulationPercent).HasPrecision(5, 2);
        });

        modelBuilder.Entity<HistoryEvent>(entity =>
        {
            entity.ToTable("HistoryEvents");
            entity.Property<int>("Id");
            entity.HasKey("Id");
            entity.Property(historyEvent => historyEvent.SectorId);
            entity.Property(historyEvent => historyEvent.EmpireId);
            entity.Property(historyEvent => historyEvent.ColonyId);
            entity.Property(historyEvent => historyEvent.EventType).HasMaxLength(80);
            entity.Property(historyEvent => historyEvent.Description).HasMaxLength(1_200);
            entity.Property(historyEvent => historyEvent.ImportDataJson);
        });
    }

    private static void ConfigureMedia(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntityImage>(entity =>
        {
            entity.ToTable("EntityImages");
            entity.HasKey(image => image.Id);
            entity.Property(image => image.TargetKind).HasConversion<string>().HasMaxLength(40);
            entity.Property(image => image.FileName).HasMaxLength(260);
            entity.Property(image => image.ContentType).HasMaxLength(80);
            entity.Property(image => image.RelativePath).HasMaxLength(500);
            entity.Property(image => image.Caption).HasMaxLength(260);
            entity.HasIndex(image => new { image.TargetKind, image.TargetId });
        });
    }

    private static void ConfigureNotes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntityNote>(entity =>
        {
            entity.ToTable("EntityNotes");
            entity.HasKey(note => note.Id);
            entity.Property(note => note.TargetKind).HasConversion<string>().HasMaxLength(40);
            entity.Property(note => note.Markdown).HasMaxLength(16_000);
            entity.HasIndex(note => new { note.TargetKind, note.TargetId }).IsUnique();
        });
    }

    private static Coordinates ParseCoordinates(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            return default;
        }

        return new Coordinates(
            short.Parse(parts[0]),
            short.Parse(parts[1]),
            short.Parse(parts[2]));
    }
}
