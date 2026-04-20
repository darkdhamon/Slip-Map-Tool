using System.IO.Compression;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services.LegacyImport;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinLegacyImportService(StarWinDbContext dbContext) : IStarWinLegacyImportService
{
    private const int StarRecordSize = 196;
    private const int PlanetRecordSize = 84;
    private const int MoonRecordSize = 60;
    private const int AlienRecordSize = 92;
    private const int ColonyRecordSize = 28;
    private const int ContactRecordSize = 6;
    private const int EmpireRecordSize = 52;
    private readonly StarWinClassificationCatalog classificationCatalog = new();
    private readonly IndependentColonyEmpireFactory independentColonyEmpireFactory = new();
    private readonly SpaceHabitatConstructionService spaceHabitatConstructionService = new();

    private static readonly string[] AtmosphereTypes =
    [
        "Massive",
        "Dense",
        "Standard",
        "Thin",
        "Very thin",
        "Vacuum"
    ];

    private static readonly string[] WorldTypes =
    [
        "Ice Ball",
        "Rock",
        "Gas Giant",
        "Hot House",
        "Failed Core",
        "Asteroid Belt",
        "Chunk",
        "Arid",
        "Steppe",
        "Terran",
        "Jungle",
        "Ocean",
        "Desert",
        "Glacier",
        "Nickel-Iron",
        "Stony",
        "Carbonaceous",
        "Icy",
        "Ring",
        "Brown Dwarf",
        "Post Garden",
        "Pre Garden",
        "Tundra"
    ];

    private static readonly string[] WaterTypes =
    [
        "None",
        "Rare Ice",
        "Ice",
        "Crystals",
        "Oceans",
        "Ice Sheets"
    ];

    private static readonly string[] AtmosphereCompositions =
    [
        "None",
        "Nitrogen/Oxygen",
        "Carbon Dioxyde",
        "Nitrogen",
        "Chlorine",
        "Methane/Ammonia/Hydrogen",
        "Ammonia",
        "Methane",
        "Hydrogen Peroxyde/Nitrogen",
        "Exotic",
        "Carbon Dioxyde/Sulfur Dioxyde",
        "Nitrogen/Chlorine",
        "Nitric Acid/Carbon Dioxyde",
        "Hydrogen Peroxyde/Hydrogen Sulfide",
        "Nitrogen/Carbon Dioxyde",
        "Methane/Ammonia",
        "Chlorine/Carbon Dioxyde",
        "Chlorine/Disulfur Dichloride",
        "Nitrogen/Sulfuric Acid",
        "Hydrogen",
        "Methane/Water Vapor"
    ];

    private static readonly string[] UnusualCharacteristics =
    [
        "Extreme Vulcanism",
        "Atmos. Contaminants",
        "Meteors Storms",
        "High Radiation Level",
        "Violent Storms",
        "Microbes",
        "Orbital Conjunction",
        "Rugged Terrain",
        "Retrograde Rotation",
        "Unstable Climate",
        "Orbital Eccentricity",
        "Unstable World",
        "Strong Magnetic Field",
        "Cloud Cover",
        "No Axial Tilt",
        "High Tides",
        "Tidal Lock",
        "Extreme Axial Tilt",
        "Int. Lifeforms",
        "Semi-Int. Lifeforms",
        "High Humidity",
        "Low Humidity",
        "Corrosive Atmosphere",
        "Insidious Atmosphere",
        "Twin World",
        "Roche World",
        "Climatic Vortex",
        "Alien artifacts",
        "Recent city ruins",
        "Remains of dead civ.",
        "Space cemetery",
        "Wonder of the galaxy",
        "Holy site",
        "Proto-organisms",
        "Primitive Lifeforms",
        "High population",
        "Terraformed",
        "u38",
        "u39",
        "u40"
    ];

    private static readonly string[] EnvironmentTypes =
    [
        "Land-dwelling",
        "Burrowing",
        "Amphibious",
        "Aquatic",
        "Flying"
    ];

    private static readonly string[] BodyChemistries =
    [
        "Carbon",
        "Silicon non-crystalline",
        "Sulfur",
        "Exotic",
        "Liquid",
        "Silicon crystalline",
        "Metallic crystalline",
        "Gaseous",
        "Energy"
    ];

    private static readonly string[] BodyCoverTypes =
    [
        "Soft-skinned",
        "Thick-skinned",
        "Furred",
        "Feathered",
        "Scaled",
        "Spiny",
        "Hard-shelled",
        "Stone-skinned",
        "Miscleanous",
        "Metallic",
        "Crystal-skinned",
        "Cellulose-skinned"
    ];

    private static readonly string[] AppearanceTypes =
    [
        "Humanoid",
        "Insectoid",
        "Reptilian",
        "Canine",
        "Feline",
        "Picthinine",
        "Ursoid",
        "Vegetal",
        "Mineral",
        "Avian",
        "Amphibian",
        "Animal",
        "Totally alien",
        "Centauroid",
        "Amoebic",
        "Serpentile",
        "Energetic",
        "Mechanoid",
        "Geometric",
        "Crustacean",
        "Arachnid",
        "Porcine",
        "Rodent",
        "Liquid",
        "Gaseous",
        "Radial"
    ];

    private static readonly string[] DietTypes =
    [
        "Herbivore",
        "Omnivore",
        "Carnivore",
        "Special",
        "Parasite",
        "Solar Energy",
        "Thermal Energy",
        "No Feeding"
    ];

    private static readonly string[] ReproductionTypes =
    [
        "Asexual",
        "Hermaphroditic",
        "Two sexes",
        "Three sexes",
        "FMN"
    ];

    private static readonly string[] ReproductionMethodTypes =
    [
        "External budding",
        "Egg-laying",
        "Live-bearing",
        "Parasitic"
    ];

    private static readonly string[] GovernmentTypes =
    [
        "Anarchy",
        "Tribalism",
        "Community",
        "Democracy",
        "Balkanization",
        "Monarchy",
        "Theocracy",
        "Corporation",
        "Oligarchy",
        "Technocracy",
        "Bureaucracy",
        "Dictatorship",
        "Republic",
        "Imperialism",
        "Matriarchy",
        "Ploutocracy",
        "Gerontocracy",
        "Aristocracy",
        "Meritocracy",
        "Stochastic",
        "Utopia",
        "Federation",
        "Syndicate",
        "Computer Oligarchy",
        "Gaming Oligarchy",
        "Subjugated colony",
        "Colony"
    ];

    private static readonly string[] ReligionTypes =
    [
        "Animism",
        "Polytheism",
        "Dualism",
        "Monotheism",
        "Deism",
        "Pantheism",
        "Agnosticism",
        "Rational atheism",
        "Philosophical atheism",
        "Leader worship",
        "Multiple monotheism"
    ];

    private static readonly string[] ColonyClasses =
    [
        "Agricultural",
        "High population",
        "Industrial",
        "Mining",
        "Fluid",
        "Recreational",
        "Capital",
        "Homeworld",
        "Settlement",
        "Military",
        "Research"
    ];

    private static readonly string[] StarportTypes =
    [
        "Excellent",
        "Good",
        "Fair",
        "Primitive",
        "None"
    ];

    private static readonly string[] FacilityTypes =
    [
        "Military Base",
        "Naval Base",
        "Prison Camp",
        "Exile Camp",
        "University",
        "Military Academy",
        "Arcology",
        "Orbital tower",
        "Ringworld",
        "Planet shield",
        "Space habitats",
        "f12",
        "f13",
        "f14",
        "f15",
        "f16"
    ];

    private static readonly string[] EconomicResourceTypes =
    [
        "None",
        "Agricultural resources",
        "Mineral resources",
        "Compounds",
        "Agroproducts",
        "Processed ores",
        "Processed compounds",
        "Weapons",
        "Consumables",
        "Pharmaceuticals",
        "Durable goods",
        "Hi-Tech goods",
        "Artforms",
        "Recordings",
        "Software",
        "Scientific datas",
        "Exotic natural resource",
        "Prototypes Mfd goods",
        "Uniques"
    ];

    private static readonly string[] RelationTypes =
    [
        "War",
        "No intercourse",
        "Trade",
        "Alliance",
        "Unity"
    ];

    private static readonly string[] RequiredExtensions =
    [
        ".sun",
        ".pln",
        ".mon",
        ".aln",
        ".col",
        ".con",
        ".emp",
        ".his",
        ".nam"
    ];

    private static readonly string[] OptionalExtensions =
    [
        ".csv",
        ".cmt"
    ];

    private static readonly HashSet<string> RecognizedExtensions = RequiredExtensions
        .Concat(OptionalExtensions)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<StarWinLegacyImportPreview> PreviewStarWinZipAsync(
        Stream zipPackage,
        string packageName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(zipPackage);

        if (!string.Equals(Path.GetExtension(packageName), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            return new StarWinLegacyImportPreview(
                packageName,
                [],
                [],
                ["StarWin 2 file imports must be uploaded as a .zip package."]);
        }

        await using var bufferedPackage = new MemoryStream();
        await zipPackage.CopyToAsync(bufferedPackage, cancellationToken);
        bufferedPackage.Position = 0;

        using var archive = new ZipArchive(bufferedPackage, ZipArchiveMode.Read, leaveOpen: true);
        var files = archive.Entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .Select(CreateFileEntry)
            .OrderBy(entry => entry.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var recognizedFiles = files
            .Where(file => file.IsRecognized)
            .ToList();

        var sectors = recognizedFiles
            .GroupBy(file => file.SectorName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Any(file => RequiredExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)))
            .Select(group => CreateSectorPreview(group, archive))
            .OrderBy(sector => sector.SectorName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sectorFileNames = sectors
            .SelectMany(sector => sector.Files.Select(file => file.EntryName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unmatchedFiles = files
            .Where(file => !sectorFileNames.Contains(file.EntryName))
            .ToList();

        var messages = new List<string>();
        if (sectors.Count == 0)
        {
            messages.Add("No StarWin sector file group was found in the zip package.");
        }

        var unrecognizedCount = files.Count(file => !file.IsRecognized);
        if (unrecognizedCount > 0)
        {
            messages.Add($"{unrecognizedCount} file(s) are not part of the known StarWin 2 sector file set and will be ignored for now.");
        }

        return new StarWinLegacyImportPreview(packageName, sectors, unmatchedFiles, messages);
    }

    public async Task<StarWinLegacyImportResult> ImportStarWinZipAsync(
        Stream zipPackage,
        string packageName,
        string targetSectorName,
        IProgress<StarWinLegacyImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(zipPackage);

        await ReportImportProgressAsync(progress, 3, "Buffering package...", "Copying the uploaded archive into an import workspace.");
        await using var bufferedPackage = new MemoryStream();
        await zipPackage.CopyToAsync(bufferedPackage, cancellationToken);

        bufferedPackage.Position = 0;
        await ReportImportProgressAsync(progress, 8, "Previewing package...", "Checking required StarWin files and record counts.");
        var preview = await PreviewStarWinZipAsync(bufferedPackage, packageName, cancellationToken);
        if (!preview.CanImport)
        {
            return new StarWinLegacyImportResult(false, null, preview, ["The package cannot be imported until required files are present."]);
        }

        bufferedPackage.Position = 0;
        using var archive = new ZipArchive(bufferedPackage, ZipArchiveMode.Read, leaveOpen: true);
        var sectorPreview = preview.Sectors[0];
        await ReportImportProgressAsync(progress, 12, "Opening sector files...", $"Opening {sectorPreview.SectorName} legacy data files from the archive.");
        var sunFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".sun", StringComparison.OrdinalIgnoreCase));
        var planetFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".pln", StringComparison.OrdinalIgnoreCase));
        var moonFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".mon", StringComparison.OrdinalIgnoreCase));
        var alienFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".aln", StringComparison.OrdinalIgnoreCase));
        var colonyFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".col", StringComparison.OrdinalIgnoreCase));
        var contactFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".con", StringComparison.OrdinalIgnoreCase));
        var empireFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".emp", StringComparison.OrdinalIgnoreCase));
        var historyFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".his", StringComparison.OrdinalIgnoreCase));
        var historyCsvFile = sectorPreview.Files.FirstOrDefault(file => string.Equals(file.Extension, ".csv", StringComparison.OrdinalIgnoreCase));
        var sunEntry = archive.GetEntry(sunFile.EntryName);
        var planetEntry = archive.GetEntry(planetFile.EntryName);
        var moonEntry = archive.GetEntry(moonFile.EntryName);
        var alienEntry = archive.GetEntry(alienFile.EntryName);
        var colonyEntry = archive.GetEntry(colonyFile.EntryName);
        var contactEntry = archive.GetEntry(contactFile.EntryName);
        var empireEntry = archive.GetEntry(empireFile.EntryName);
        var historyEntry = archive.GetEntry(historyCsvFile?.EntryName ?? historyFile.EntryName);
        if (sunEntry is null)
        {
            return new StarWinLegacyImportResult(false, null, preview, ["The .sun file could not be opened from the zip package."]);
        }

        if (planetEntry is null)
        {
            return new StarWinLegacyImportResult(false, null, preview, ["The .pln file could not be opened from the zip package."]);
        }

        if (moonEntry is null)
        {
            return new StarWinLegacyImportResult(false, null, preview, ["The .mon file could not be opened from the zip package."]);
        }

        if (alienEntry is null)
        {
            return new StarWinLegacyImportResult(false, null, preview, ["The .aln file could not be opened from the zip package."]);
        }

        if (colonyEntry is null)
        {
            return new StarWinLegacyImportResult(false, null, preview, ["The .col file could not be opened from the zip package."]);
        }

        if (contactEntry is null)
        {
            return new StarWinLegacyImportResult(false, null, preview, ["The .con file could not be opened from the zip package."]);
        }

        if (empireEntry is null)
        {
            return new StarWinLegacyImportResult(false, null, preview, ["The .emp file could not be opened from the zip package."]);
        }

        if (historyEntry is null)
        {
            return new StarWinLegacyImportResult(false, null, preview, ["The history file could not be opened from the zip package."]);
        }

        var requestedSectorName = NormalizeSectorName(
            string.IsNullOrWhiteSpace(targetSectorName) ? sectorPreview.SectorName : targetSectorName);
        await ReportImportProgressAsync(progress, 18, "Loading sector state...", $"Checking whether {requestedSectorName} already exists.");
        var sector = await dbContext.Sectors
            .FirstOrDefaultAsync(existingSector => existingSector.Name == requestedSectorName, cancellationToken);
        var isNewSector = sector is null;
        if (sector is null)
        {
            sector = new StarWinSector
            {
                Id = await GetNextSectorIdAsync(cancellationToken),
                Name = requestedSectorName
            };
            sector.Configuration.SectorId = sector.Id;
        }

        var sectorId = sector.Id;
        await ReportImportProgressAsync(progress, 24, "Reading existing records...", "Loading existing systems, worlds, races, empires, colonies, and history keys.");
        var existingSystemIds = isNewSector
            ? new HashSet<int>()
            : await dbContext.StarSystems
                .Where(system => system.SectorId == sectorId)
                .Select(system => system.Id)
                .ToHashSetAsync(cancellationToken);
        var existingWorldIds = isNewSector
            ? new HashSet<int>()
            : await dbContext.Worlds
                .Where(world => world.StarSystemId != null
                    && dbContext.StarSystems.Any(system => system.Id == world.StarSystemId && system.SectorId == sectorId))
                .Select(world => world.Id)
                .ToHashSetAsync(cancellationToken);
        var existingAstralBodyRoles = isNewSector
            ? new Dictionary<int, HashSet<AstralBodyRole>>()
            : (await dbContext.Set<AstralBody>()
                .Where(body => dbContext.StarSystems.Any(system =>
                    system.Id == EF.Property<int>(body, "StarSystemId") && system.SectorId == sectorId))
                .Select(body => new
                {
                    StarSystemId = EF.Property<int>(body, "StarSystemId"),
                    body.Role
                })
                .ToListAsync(cancellationToken))
                .GroupBy(body => body.StarSystemId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(body => body.Role).ToHashSet());
        var existingRaceIds = await dbContext.AlienRaces
            .Select(race => race.Id)
            .ToHashSetAsync(cancellationToken);
        var existingEmpireIds = await dbContext.Empires
            .Select(empire => empire.Id)
            .ToHashSetAsync(cancellationToken);
        var knownAlienRaces = await dbContext.AlienRaces
            .ToDictionaryAsync(race => race.Id, cancellationToken);
        var knownEmpires = await dbContext.Empires
            .ToDictionaryAsync(empire => empire.Id, cancellationToken);
        var independentEmpireColonyIds = await dbContext.Empires
            .Where(empire => empire.Founding.Origin == EmpireOrigin.IndependentColony
                && empire.Founding.FoundingColonyId != null)
            .Select(empire => empire.Founding.FoundingColonyId!.Value)
            .ToHashSetAsync(cancellationToken);
        var existingSpaceHabitatWorldIds = await dbContext.SpaceHabitats
            .Where(habitat => habitat.OrbitTargetKind == OrbitTargetKind.World)
            .Select(habitat => habitat.OrbitTargetId)
            .ToHashSetAsync(cancellationToken);
        var existingColonyWorldIds = await dbContext.Colonies
            .Select(colony => colony.WorldId)
            .ToHashSetAsync(cancellationToken);
        var existingHistoryKeys = new HashSet<string>(StringComparer.Ordinal);
        if (!isNewSector)
        {
            var existingHistory = await dbContext.HistoryEvents
                .Where(history => history.SectorId == sectorId)
                .ToListAsync(cancellationToken);
            foreach (var history in existingHistory)
            {
                existingHistoryKeys.Add(BuildHistoryKey(history));
            }
        }

        await ReportImportProgressAsync(progress, 32, "Reading legacy records...", "Parsing StarWin systems, planets, moons, races, empires, colonies, contacts, and history.");
        var importedSystems = ReadStarSystems(sunEntry, sectorId, sectorPreview.StarSystemRecordCount)
            .ToList();
        var planets = ReadPlanetRecords(planetEntry).ToList();
        var moons = ReadMoonRecords(moonEntry).ToList();
        var aliens = ReadAlienRecords(alienEntry).ToList();
        var empires = ReadEmpireRecords(empireEntry).ToList();
        var colonies = ReadColonyRecords(colonyEntry).ToList();
        var contacts = ReadContactRecords(contactEntry).ToList();
        var historyEvents = ReadHistoryEvents(historyEntry, sectorId, historyCsvFile is not null).ToList();
        var addedSystemCount = 0;
        var addedAstralBodyCount = 0;
        var addedWorldCount = 0;
        var addedMoonCount = 0;
        var addedAlienCount = 0;
        var addedEmpireCount = 0;
        var addedColonyCount = 0;
        var addedContactCount = 0;
        var addedHistoryCount = 0;
        var addedIndependentEmpireCount = 0;
        var addedSpaceHabitatCount = 0;
        var skippedSystemCount = 0;
        var skippedWorldCount = 0;
        var skippedAlienCount = 0;
        var skippedEmpireCount = 0;
        var skippedColonyCount = 0;
        var skippedHistoryCount = 0;
        var originalAutoDetectChanges = dbContext.ChangeTracker.AutoDetectChangesEnabled;
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        await using var importTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (isNewSector)
            {
                dbContext.Sectors.Add(sector);
            }

            var importedWorldsById = new Dictionary<int, World>();
            await ReportImportProgressAsync(progress, 42, "Writing star systems and worlds...", $"Preparing {importedSystems.Count:N0} star system record(s), {planets.Count:N0} planet record(s), and {moons.Count:N0} moon record(s).");
            var processedSystemCount = 0;
            foreach (var systemImport in importedSystems)
            {
                processedSystemCount++;
                if (processedSystemCount == 1 || processedSystemCount % 250 == 0 || processedSystemCount == importedSystems.Count)
                {
                    var systemPercent = CalculatePhasePercent(42, 58, processedSystemCount, importedSystems.Count);
                    await ReportImportProgressAsync(progress, systemPercent, "Writing star systems and worlds...", $"Processed {processedSystemCount:N0} of {importedSystems.Count:N0} star system record(s).");
                }

                var worlds = BuildWorlds(systemImport, planets, moons, sectorId);
                var newWorlds = worlds
                    .Where(world => !existingWorldIds.Contains(world.Id))
                    .ToList();
                skippedWorldCount += worlds.Count - newWorlds.Count;
                foreach (var world in newWorlds)
                {
                    existingWorldIds.Add(world.Id);
                    importedWorldsById[world.Id] = world;
                }

                if (existingSystemIds.Add(systemImport.System.Id))
                {
                    foreach (var world in newWorlds)
                    {
                        systemImport.System.Worlds.Add(world);
                    }

                    dbContext.StarSystems.Add(systemImport.System);
                    addedSystemCount++;
                    addedAstralBodyCount += systemImport.System.AstralBodies.Count;
                    addedWorldCount += newWorlds.Count;
                    addedMoonCount += newWorlds.Count(world => world.Kind == WorldKind.Moon);
                    continue;
                }

                skippedSystemCount++;
                if (!existingAstralBodyRoles.TryGetValue(systemImport.System.Id, out var existingRoles))
                {
                    existingRoles = [];
                    existingAstralBodyRoles[systemImport.System.Id] = existingRoles;
                }

                foreach (var astralBody in systemImport.System.AstralBodies.Where(body => existingRoles.Add(body.Role)))
                {
                    dbContext.Set<AstralBody>().Add(astralBody);
                    dbContext.Entry(astralBody).Property("StarSystemId").CurrentValue = systemImport.System.Id;
                    addedAstralBodyCount++;
                }

                foreach (var world in newWorlds)
                {
                    dbContext.Worlds.Add(world);
                }

                addedWorldCount += newWorlds.Count;
                addedMoonCount += newWorlds.Count(world => world.Kind == WorldKind.Moon);
            }

            await FlushImportChangesAsync(
                progress,
                59,
                "Saving star systems and worlds...",
                $"Committing {addedSystemCount:N0} star system(s), {addedAstralBodyCount:N0} astral bodie(s), and {addedWorldCount:N0} world(s).",
                cancellationToken);

            await ReportImportProgressAsync(progress, 60, "Writing alien races...", $"Preparing {aliens.Count:N0} alien race record(s).");
            foreach (var alienRecord in aliens)
            {
                if (!existingRaceIds.Add(alienRecord.LegacyId))
                {
                    skippedAlienCount++;
                    continue;
                }

                var alienRace = CreateAlienRace(alienRecord, sectorId);
                dbContext.AlienRaces.Add(alienRace);
                knownAlienRaces[alienRace.Id] = alienRace;
                addedAlienCount++;
            }

            await ReportImportProgressAsync(progress, 66, "Writing empires and contacts...", $"Preparing {empires.Count:N0} empire record(s) and {contacts.Count:N0} contact record(s).");
            foreach (var empireRecord in empires)
            {
                if (!existingEmpireIds.Add(empireRecord.LegacyId))
                {
                    skippedEmpireCount++;
                    continue;
                }

                var empire = CreateEmpire(empireRecord, aliens, contacts, sectorId);
                dbContext.Empires.Add(empire);
                knownEmpires[empire.Id] = empire;
                addedEmpireCount++;
                addedContactCount += empire.Contacts.Count;
            }

            await FlushImportChangesAsync(
                progress,
                71,
                "Saving races, empires, and contacts...",
                $"Committing {addedAlienCount:N0} alien race(s), {addedEmpireCount:N0} empire(s), and {addedContactCount:N0} contact(s).",
                cancellationToken);

            var importedColonies = new List<Colony>();
            await ReportImportProgressAsync(progress, 72, "Writing colonies...", $"Preparing {colonies.Count:N0} colony record(s).");
            foreach (var colonyRecord in colonies)
            {
                var colony = CreateColony(colonyRecord, aliens, sectorId);
                if (!existingWorldIds.Contains(colony.WorldId) || !existingColonyWorldIds.Add(colony.WorldId))
                {
                    skippedColonyCount++;
                    continue;
                }

                dbContext.Colonies.Add(colony);
                importedColonies.Add(colony);
                addedColonyCount++;
            }

            await ReportImportProgressAsync(progress, 78, "Writing history events...", $"Preparing {historyEvents.Count:N0} history event record(s).");
            foreach (var historyEvent in historyEvents)
            {
                var historyKey = BuildHistoryKey(historyEvent);
                if (!existingHistoryKeys.Add(historyKey))
                {
                    skippedHistoryCount++;
                    continue;
                }

                dbContext.HistoryEvents.Add(historyEvent);
                addedHistoryCount++;
            }

            await FlushImportChangesAsync(
                progress,
                83,
                "Saving colonies and history...",
                $"Committing {addedColonyCount:N0} colonie(s) and {addedHistoryCount:N0} history event(s).",
                cancellationToken);

            await ReportImportProgressAsync(progress, 84, "Deriving colony state...", "Loading sector colonies, worlds, and history for independent colony analysis.");
            var sectorColonies = await dbContext.Colonies
                .Where(colony => dbContext.Worlds.Any(world => world.Id == colony.WorldId
                    && world.StarSystemId != null
                    && dbContext.StarSystems.Any(system => system.Id == world.StarSystemId && system.SectorId == sectorId)))
                .Include(colony => colony.Demographics)
                .ToListAsync(cancellationToken);

            await ReportImportProgressAsync(progress, 85, "Deriving colony state...", $"Loaded {sectorColonies.Count:N0} sector colonie(s). Loading sector worlds.");
            var sectorWorlds = await dbContext.Worlds
                .Where(world => world.StarSystemId != null
                    && dbContext.StarSystems.Any(system => system.Id == world.StarSystemId && system.SectorId == sectorId))
                .ToDictionaryAsync(world => world.Id, cancellationToken);

            await ReportImportProgressAsync(progress, 86, "Deriving colony state...", $"Loaded {sectorWorlds.Count:N0} sector world(s). Loading sector history.");
            var sectorHistory = await dbContext.HistoryEvents
                .Where(history => history.SectorId == sectorId)
                .ToListAsync(cancellationToken);
            var independenceEvents = BuildIndependenceEventIndex(sectorHistory);

            await ReportImportProgressAsync(progress, 88, "Creating independent empires...", $"Checking {sectorColonies.Count:N0} colonie(s) against indexed independence history.");
            var nextEmpireId = Math.Max(
                existingEmpireIds.Count == 0 ? 0 : existingEmpireIds.Max(),
                knownEmpires.Count == 0 ? 0 : knownEmpires.Keys.Max()) + 1;
            var processedColonyStateCount = 0;
            foreach (var colony in sectorColonies)
            {
                processedColonyStateCount++;
                if (processedColonyStateCount == 1 || processedColonyStateCount % 250 == 0 || processedColonyStateCount == sectorColonies.Count)
                {
                    var colonyStatePercent = CalculatePhasePercent(88, 91, processedColonyStateCount, sectorColonies.Count);
                    await ReportImportProgressAsync(progress, colonyStatePercent, "Creating independent empires...", $"Checked {processedColonyStateCount:N0} of {sectorColonies.Count:N0} colonie(s).");
                }

                if (!IsIndependentColony(colony, independenceEvents))
                {
                    continue;
                }

                if (!independentEmpireColonyIds.Add(colony.Id)
                    || !sectorWorlds.TryGetValue(colony.WorldId, out var world)
                    || !knownAlienRaces.TryGetValue(colony.RaceId, out var foundingRace))
                {
                    continue;
                }

                var parentEmpire = ResolveEmpire(knownEmpires, colony.ParentEmpireId ?? colony.FoundingEmpireId);
                var independentEmpire = independentColonyEmpireFactory.CreateEmpireFromIndependentColony(
                    colony,
                    world,
                    foundingRace,
                    parentEmpire);
                while (!existingEmpireIds.Add(nextEmpireId))
                {
                    nextEmpireId++;
                }

                independentEmpire.Id = nextEmpireId++;
                independentEmpire.Founding.FoundedCentury = FindIndependenceCentury(colony, independenceEvents);
                AssignColonyToIndependentEmpire(colony, independentEmpire);
                dbContext.Empires.Add(independentEmpire);
                knownEmpires[independentEmpire.Id] = independentEmpire;
                addedIndependentEmpireCount++;
            }

            await ReportImportProgressAsync(progress, 92, "Creating space habitats...", "Creating habitat satellites from imported colony facilities.");
            var nextSpaceHabitatId = (await dbContext.SpaceHabitats
                .Select(habitat => (int?)habitat.Id)
                .MaxAsync(cancellationToken) ?? 0) + 1;
            foreach (var colony in importedColonies.Where(HasSpaceHabitatFacility))
            {
                if (!existingSpaceHabitatWorldIds.Add(colony.WorldId)
                    || !sectorWorlds.TryGetValue(colony.WorldId, out var world))
                {
                    continue;
                }

                var spaceHabitat = CreateSpaceHabitat(colony, world, knownEmpires);
                spaceHabitat.Id = nextSpaceHabitatId++;
                dbContext.SpaceHabitats.Add(spaceHabitat);
                if (world.StarSystemId is { } starSystemId)
                {
                    dbContext.Entry(spaceHabitat).Property("StarSystemId").CurrentValue = starSystemId;
                }

                addedSpaceHabitatCount++;
            }

            await ReportImportProgressAsync(progress, 96, "Preparing import summary...", "Recording the import history event and final change summary.");
            if (addedSystemCount > 0 || addedAstralBodyCount > 0 || addedWorldCount > 0 || addedAlienCount > 0 || addedEmpireCount > 0 || addedColonyCount > 0 || addedHistoryCount > 0 || addedIndependentEmpireCount > 0 || addedSpaceHabitatCount > 0)
            {
                dbContext.HistoryEvents.Add(new HistoryEvent
                {
                    SectorId = sector.Id,
                    Century = 0,
                    EventType = "Legacy Import",
                    Description = $"Merged {addedSystemCount:N0} missing star systems, {addedAstralBodyCount:N0} missing astral bodies, {addedWorldCount:N0} missing worlds, {addedAlienCount:N0} alien races, {addedEmpireCount:N0} empires, {addedColonyCount:N0} colonies, {addedHistoryCount:N0} history events, {addedIndependentEmpireCount:N0} independent colony empires, and {addedSpaceHabitatCount:N0} space habitats from {packageName}. Existing records were not overwritten."
                });
            }

            await FlushImportChangesAsync(
                progress,
                100,
                "Saving derived records...",
                $"Committing {addedIndependentEmpireCount:N0} independent empire(s), {addedSpaceHabitatCount:N0} habitat(s), and the import summary.",
                cancellationToken,
                clearChangeTracker: false);
            await importTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            dbContext.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
        }

        return new StarWinLegacyImportResult(
            true,
            sector.Id,
            preview,
            [
                $"{sector.Name} merged from {packageName}.",
                $"Added {addedSystemCount:N0} star systems, {addedAstralBodyCount:N0} astral bodies, {addedWorldCount:N0} worlds, and {addedMoonCount:N0} moon satellites.",
                $"Added {addedAlienCount:N0} alien races, {addedEmpireCount:N0} empires, {addedContactCount:N0} empire contacts, {addedColonyCount:N0} colonies, and {addedHistoryCount:N0} history events.",
                $"Created {addedIndependentEmpireCount:N0} independent colony empires and {addedSpaceHabitatCount:N0} space habitat satellites from colony/history data.",
                $"Skipped {skippedSystemCount:N0} existing star systems, {skippedWorldCount:N0} existing worlds, {skippedAlienCount:N0} races, {skippedEmpireCount:N0} empires, {skippedColonyCount:N0} colonies, and {skippedHistoryCount:N0} history events without overwriting."
            ]);
    }

    private static async Task ReportImportProgressAsync(
        IProgress<StarWinLegacyImportProgress>? progress,
        int percentComplete,
        string status,
        string detail)
    {
        progress?.Report(new StarWinLegacyImportProgress(
            Math.Clamp(percentComplete, 0, 100),
            status,
            detail));

        await Task.Yield();
    }

    private async Task FlushImportChangesAsync(
        IProgress<StarWinLegacyImportProgress>? progress,
        int percentComplete,
        string status,
        string detail,
        CancellationToken cancellationToken,
        bool clearChangeTracker = true)
    {
        dbContext.ChangeTracker.DetectChanges();
        var pendingChanges = dbContext.ChangeTracker
            .Entries()
            .Count(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
        await ReportImportProgressAsync(
            progress,
            Math.Clamp(percentComplete - 1, 0, 100),
            status,
            $"{detail} {pendingChanges:N0} pending database change(s).");

        await dbContext.SaveChangesAsync(cancellationToken);

        if (clearChangeTracker)
        {
            dbContext.ChangeTracker.Clear();
        }

        await ReportImportProgressAsync(progress, percentComplete, status, "Database changes committed.");
    }

    private static int CalculatePhasePercent(int startPercent, int endPercent, int processedCount, int totalCount)
    {
        if (totalCount <= 0)
        {
            return endPercent;
        }

        var phaseWidth = endPercent - startPercent;
        return startPercent + (int)Math.Round(phaseWidth * Math.Clamp(processedCount / (double)totalCount, 0, 1));
    }

    private static StarWinLegacyImportFileEntry CreateFileEntry(ZipArchiveEntry entry)
    {
        var extension = Path.GetExtension(entry.Name).ToLowerInvariant();
        var sectorName = GetSectorName(entry);

        return new StarWinLegacyImportFileEntry(
            entry.FullName,
            entry.Name,
            sectorName,
            extension,
            entry.Length,
            RecognizedExtensions.Contains(extension));
    }

    private static string GetSectorName(ZipArchiveEntry entry)
    {
        var pathParts = entry.FullName
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return pathParts.Length >= 2
            ? pathParts[^2]
            : Path.GetFileNameWithoutExtension(entry.Name);
    }

    private static StarWinLegacyImportSectorPreview CreateSectorPreview(
        IGrouping<string, StarWinLegacyImportFileEntry> sectorFiles,
        ZipArchive archive)
    {
        var files = sectorFiles
            .OrderBy(file => file.Extension, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var presentExtensions = files
            .Select(file => file.Extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingRequired = RequiredExtensions
            .Where(extension => !presentExtensions.Contains(extension))
            .ToList();

        var missingOptional = OptionalExtensions
            .Where(extension => !presentExtensions.Contains(extension))
            .ToList();

        var messages = new List<string>();
        if (missingRequired.Count == 0)
        {
            messages.Add("All required StarWin 2 sector files are present.");
        }
        else
        {
            messages.Add($"Missing required file type(s): {string.Join(", ", missingRequired)}.");
        }

        if (files.Any(file => file.Length == 0))
        {
            messages.Add("One or more files are empty. Empty comment files are normal; empty data files need review.");
        }

        var sampleStarSystems = ReadSampleStarSystems(archive, files, out var starSystemRecordCount);
        if (starSystemRecordCount > 0)
        {
            messages.Add($"{starSystemRecordCount:N0} star system record(s) found in the .sun file.");
        }

        AddRecordCountMessage(messages, files, ".aln", AlienRecordSize, "alien race");
        AddRecordCountMessage(messages, files, ".emp", EmpireRecordSize, "empire");
        AddRecordCountMessage(messages, files, ".col", ColonyRecordSize, "colony");
        AddRecordCountMessage(messages, files, ".con", ContactRecordSize, "empire contact");
        AddLineCountMessage(messages, archive, files, ".csv", "structured history event");

        return new StarWinLegacyImportSectorPreview(
            sectorFiles.Key,
            missingRequired.Count == 0,
            files,
            missingRequired,
            missingOptional,
            starSystemRecordCount,
            sampleStarSystems,
            messages);
    }

    private static void AddRecordCountMessage(
        ICollection<string> messages,
        IReadOnlyList<StarWinLegacyImportFileEntry> files,
        string extension,
        int recordSize,
        string label)
    {
        var file = files.FirstOrDefault(file => string.Equals(file.Extension, extension, StringComparison.OrdinalIgnoreCase));
        if (file is null || file.Length < recordSize)
        {
            return;
        }

        messages.Add($"{file.Length / recordSize:N0} {label} record(s) found in the {extension} file.");
    }

    private static void AddLineCountMessage(
        ICollection<string> messages,
        ZipArchive archive,
        IReadOnlyList<StarWinLegacyImportFileEntry> files,
        string extension,
        string label)
    {
        var file = files.FirstOrDefault(file => string.Equals(file.Extension, extension, StringComparison.OrdinalIgnoreCase));
        var entry = file is null ? null : archive.GetEntry(file.EntryName);
        if (entry is null)
        {
            return;
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream, System.Text.Encoding.Latin1, detectEncodingFromByteOrderMarks: true);
        var lineCount = 0;
        while (reader.ReadLine() is not null)
        {
            lineCount++;
        }

        if (lineCount > 1)
        {
            messages.Add($"{lineCount - 1:N0} {label}(s) found in the {extension} file.");
        }
    }

    private static IReadOnlyList<StarWinLegacyStarSystemPreview> ReadSampleStarSystems(
        ZipArchive archive,
        IReadOnlyList<StarWinLegacyImportFileEntry> files,
        out int recordCount)
    {
        const int sampleLimit = 8;

        recordCount = 0;
        var starFile = files.FirstOrDefault(file => string.Equals(file.Extension, ".sun", StringComparison.OrdinalIgnoreCase));
        if (starFile is null)
        {
            return [];
        }

        var entry = archive.GetEntry(starFile.EntryName);
        if (entry is null || entry.Length < StarRecordSize)
        {
            return [];
        }

        recordCount = (int)(entry.Length / StarRecordSize);
        var samples = new List<StarWinLegacyStarSystemPreview>();

        using var stream = entry.Open();
        var buffer = new byte[StarRecordSize];
        for (var legacyId = 0; legacyId < recordCount && samples.Count < sampleLimit; legacyId++)
        {
            if (!TryReadExact(stream, buffer))
            {
                break;
            }

            var name = ReadDelphiShortString(buffer, 173, 20);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var starCount = buffer[150];
            samples.Add(new StarWinLegacyStarSystemPreview(
                legacyId,
                name,
                BitConverter.ToInt16(buffer, 164),
                BitConverter.ToInt16(buffer, 166),
                BitConverter.ToInt16(buffer, 168),
                starCount,
                [buffer[6], buffer[7], buffer[8]],
                BuildSpectralClasses(buffer, starCount),
                BitConverter.ToUInt16(buffer, 170)));
        }

        return samples;
    }

    private IEnumerable<LegacyStarSystemImport> ReadStarSystems(ZipArchiveEntry entry, int sectorId, int sourceRecordCount)
    {
        using var stream = entry.Open();
        var buffer = new byte[StarRecordSize];
        for (var legacyId = 0; legacyId < sourceRecordCount; legacyId++)
        {
            if (!TryReadExact(stream, buffer))
            {
                yield break;
            }

            var name = ReadDelphiShortString(buffer, 173, 20);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var system = new StarSystem
            {
                Id = sectorId * 100_000 + legacyId + 1,
                SectorId = sectorId,
                LegacySystemId = legacyId,
                Name = name,
                Coordinates = new Coordinates(
                    BitConverter.ToInt16(buffer, 164),
                    BitConverter.ToInt16(buffer, 166),
                    BitConverter.ToInt16(buffer, 168)),
                AllegianceId = BitConverter.ToUInt16(buffer, 170),
                MapCode = buffer[172]
            };

            var starCount = Math.Clamp(buffer[150], (byte)1, (byte)3);
            for (var index = 0; index < starCount; index++)
            {
                var classificationCode = buffer[index];
                var decimalClassCode = buffer[3 + index];
                system.AstralBodies.Add(new AstralBody
                {
                    Role = index switch
                    {
                        0 => AstralBodyRole.Primary,
                        1 => AstralBodyRole.FirstCompanion,
                        _ => AstralBodyRole.SecondCompanion
                    },
                    Kind = classificationCatalog.GetAstralBodyKind(classificationCode),
                    ClassificationCode = classificationCode,
                    DecimalClassCode = decimalClassCode,
                    Classification = classificationCatalog.GetAstralBodyClassification(classificationCode, decimalClassCode),
                    PlanetCount = buffer[6 + index],
                    Luminosity = BitConverter.ToSingle(buffer, 12 + index * 4),
                    SolarMasses = BitConverter.ToSingle(buffer, 24 + index * 4),
                    CompanionOrbitAu = index == 0 ? null : BitConverter.ToInt16(buffer, 34 + (index - 1) * 2) / 10d
                });
            }

            yield return new LegacyStarSystemImport(
                legacyId,
                system,
                [buffer[6], buffer[7], buffer[8]],
                [
                    BitConverter.ToInt32(buffer, 152),
                    BitConverter.ToInt32(buffer, 156),
                    BitConverter.ToInt32(buffer, 160)
                ]);
        }
    }

    private static IEnumerable<LegacyPlanetImport> ReadPlanetRecords(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var buffer = new byte[PlanetRecordSize];
        var recordCount = (int)(entry.Length / PlanetRecordSize);

        for (var legacyId = 0; legacyId < recordCount; legacyId++)
        {
            if (!TryReadExact(stream, buffer))
            {
                yield break;
            }

            yield return new LegacyPlanetImport(
                legacyId,
                BitConverter.ToInt32(buffer, 0),
                BitConverter.ToInt32(buffer, 4),
                BitConverter.ToInt32(buffer, 8),
                BitConverter.ToUInt16(buffer, 12),
                buffer[14],
                buffer[15],
                buffer[16],
                buffer[17],
                buffer[18],
                buffer[19],
                buffer[20],
                BitConverter.ToInt16(buffer, 22),
                buffer[24],
                [buffer[25], buffer[26], buffer[27], buffer[28], buffer[29]],
                BitConverter.ToInt32(buffer, 32),
                BitConverter.ToInt32(buffer, 36),
                buffer[40],
                BitConverter.ToSingle(buffer, 44),
                BitConverter.ToUInt16(buffer, 48),
                BitConverter.ToInt16(buffer, 50),
                [buffer[52], buffer[53], buffer[54], buffer[55], buffer[56]],
                buffer[57],
                [buffer[58], buffer[59], buffer[60], buffer[61], buffer[62]],
                ReadDelphiShortString(buffer, 63, 20));
        }
    }

    private static IEnumerable<LegacyMoonImport> ReadMoonRecords(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var buffer = new byte[MoonRecordSize];
        var recordCount = (int)(entry.Length / MoonRecordSize);

        for (var legacyId = 0; legacyId < recordCount; legacyId++)
        {
            if (!TryReadExact(stream, buffer))
            {
                yield break;
            }

            yield return new LegacyMoonImport(
                legacyId,
                BitConverter.ToInt32(buffer, 0),
                buffer[4],
                buffer[5],
                buffer[6],
                buffer[7],
                BitConverter.ToInt16(buffer, 8),
                [buffer[10], buffer[11], buffer[12], buffer[13], buffer[14]],
                BitConverter.ToInt32(buffer, 16),
                buffer[20],
                BitConverter.ToSingle(buffer, 24),
                BitConverter.ToUInt16(buffer, 28),
                [buffer[30], buffer[31], buffer[32], buffer[33], buffer[34]],
                buffer[35],
                ReadDelphiShortString(buffer, 36, 20));
        }
    }

    private static IEnumerable<LegacyAlienImport> ReadAlienRecords(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var buffer = new byte[AlienRecordSize];
        var recordCount = (int)(entry.Length / AlienRecordSize);

        for (var legacyId = 0; legacyId < recordCount; legacyId++)
        {
            if (!TryReadExact(stream, buffer))
            {
                yield break;
            }

            var attributes = buffer.Skip(30).Take(15).ToArray();
            yield return new LegacyAlienImport(
                legacyId,
                BitConverter.ToInt32(buffer, 0),
                buffer[4],
                buffer[5],
                buffer[6],
                buffer[7],
                buffer[8],
                buffer[9],
                buffer[10],
                buffer[11],
                buffer[12],
                BitConverter.ToInt16(buffer, 14),
                BitConverter.ToInt16(buffer, 16),
                buffer.Skip(18).Take(12).ToArray(),
                attributes,
                buffer.Skip(45).Take(12).ToArray(),
                buffer.Skip(57).Take(2).ToArray(),
                buffer.Skip(59).Take(2).ToArray(),
                buffer.Skip(61).Take(2).ToArray(),
                buffer.Skip(63).Take(2).ToArray(),
                buffer.Skip(65).Take(2).ToArray(),
                buffer[67],
                buffer[68],
                buffer[69],
                ReadDelphiShortString(buffer, 70, 20));
        }
    }

    private static IEnumerable<LegacyColonyImport> ReadColonyRecords(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var buffer = new byte[ColonyRecordSize];
        var recordCount = (int)(entry.Length / ColonyRecordSize);

        for (var legacyId = 0; legacyId < recordCount; legacyId++)
        {
            if (!TryReadExact(stream, buffer))
            {
                yield break;
            }

            yield return new LegacyColonyImport(
                legacyId,
                BitConverter.ToInt32(buffer, 0),
                BitConverter.ToUInt16(buffer, 4),
                BitConverter.ToUInt16(buffer, 6),
                buffer[8],
                buffer[9],
                buffer[10],
                buffer[11],
                buffer[12],
                buffer[13],
                buffer[14],
                buffer[15],
                buffer[16],
                BitConverter.ToUInt16(buffer, 18),
                BitConverter.ToUInt16(buffer, 20),
                buffer[22],
                [buffer[23], buffer[24]],
                buffer[25],
                buffer[26]);
        }
    }

    private static IEnumerable<LegacyEmpireImport> ReadEmpireRecords(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var buffer = new byte[EmpireRecordSize];
        var recordCount = (int)(entry.Length / EmpireRecordSize);

        for (var legacyId = 0; legacyId < recordCount; legacyId++)
        {
            if (!TryReadExact(stream, buffer))
            {
                yield break;
            }

            var attributes = new int[13];
            for (var index = 0; index < attributes.Length; index++)
            {
                attributes[index] = BitConverter.ToInt32(buffer, index * 4);
            }

            yield return new LegacyEmpireImport(legacyId, attributes);
        }
    }

    private static IEnumerable<LegacyContactImport> ReadContactRecords(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var buffer = new byte[ContactRecordSize];
        var recordCount = (int)(entry.Length / ContactRecordSize);

        for (var legacyId = 0; legacyId < recordCount; legacyId++)
        {
            if (!TryReadExact(stream, buffer))
            {
                yield break;
            }

            yield return new LegacyContactImport(
                legacyId,
                BitConverter.ToUInt16(buffer, 0),
                BitConverter.ToUInt16(buffer, 2),
                buffer[4],
                buffer[5]);
        }
    }

    private static IEnumerable<HistoryEvent> ReadHistoryEvents(ZipArchiveEntry entry, int sectorId, bool isCsv)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, System.Text.Encoding.Latin1, detectEncodingFromByteOrderMarks: true);

        if (!isCsv)
        {
            while (reader.ReadLine() is { } line)
            {
                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                yield return new HistoryEvent
                {
                    SectorId = sectorId,
                    EventType = line.StartsWith("Info", StringComparison.OrdinalIgnoreCase) ? "Info" : "History",
                    Description = Truncate(line, 1_200)
                };
            }

            yield break;
        }

        _ = reader.ReadLine();
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitSemicolonCsvLine(line);
            if (fields.Count < 7)
            {
                continue;
            }

            var legacyPlanetId = ParseNullableInt(fields[4]);
            var legacySystemId = ParseNullableInt(fields[5]);
            yield return new HistoryEvent
            {
                SectorId = sectorId,
                Century = ParseInt(fields[0]),
                EventType = string.IsNullOrWhiteSpace(fields[1]) ? "History" : fields[1].Trim(),
                RaceId = ParseNullableInt(fields[2]),
                OtherRaceId = ParseNullableInt(fields[3]),
                EmpireId = ParseNullableInt(fields[2]),
                PlanetId = legacyPlanetId is null ? null : BuildPlanetWorldId(sectorId, legacyPlanetId.Value),
                StarSystemId = legacySystemId is null ? null : BuildStarSystemId(sectorId, legacySystemId.Value),
                Description = Truncate(fields[6].Trim(), 1_200)
            };
        }
    }

    private static List<World> BuildWorlds(
        LegacyStarSystemImport systemImport,
        IReadOnlyList<LegacyPlanetImport> planets,
        IReadOnlyList<LegacyMoonImport> moons,
        int sectorId)
    {
        var worlds = new List<World>();

        for (var astralBodySequence = 0; astralBodySequence < systemImport.PlanetCounts.Length; astralBodySequence++)
        {
            var firstPlanetId = systemImport.FirstPlanetIds[astralBodySequence];
            var astralBodyPlanetCount = systemImport.PlanetCounts[astralBodySequence];
            if (firstPlanetId < 0 || astralBodyPlanetCount == 0)
            {
                continue;
            }

            for (var orbitIndex = 0; orbitIndex < astralBodyPlanetCount; orbitIndex++)
            {
                var legacyPlanetId = firstPlanetId + orbitIndex;
                if (legacyPlanetId < 0 || legacyPlanetId >= planets.Count)
                {
                    continue;
                }

                var planetRecord = planets[legacyPlanetId];
                var planet = CreatePlanetWorld(planetRecord, sectorId, systemImport.System.Id, astralBodySequence, orbitIndex);
                worlds.Add(planet);

                if (planetRecord.FirstMoonId < 0 || planetRecord.SatelliteCount == 0)
                {
                    continue;
                }

                for (var moonIndex = 0; moonIndex < planetRecord.SatelliteCount; moonIndex++)
                {
                    var legacyMoonId = planetRecord.FirstMoonId + moonIndex;
                    if (legacyMoonId < 0 || legacyMoonId >= moons.Count)
                    {
                        continue;
                    }

                    var moonRecord = moons[legacyMoonId];
                    var moon = CreateMoonWorld(moonRecord, sectorId, systemImport.System.Id, planet.Id, astralBodySequence, moonIndex);
                    worlds.Add(moon);
                }
            }
        }

        return worlds;
    }

    private static AlienRace CreateAlienRace(LegacyAlienImport record, int sectorId)
    {
        var name = string.IsNullOrWhiteSpace(record.Name)
            ? $"Race {record.LegacyId}"
            : record.Name;

        return new AlienRace
        {
            Id = record.LegacyId,
            HomePlanetId = BuildPlanetWorldId(sectorId, Math.Abs(record.HomePlanetId)),
            Name = name,
            EnvironmentType = Lookup(EnvironmentTypes, record.EnvironmentType, "Environment"),
            BodyChemistry = Lookup(BodyChemistries, record.BodyType, "Body"),
            GovernmentType = Lookup(GovernmentTypes, record.GovernmentType, "Government"),
            BodyCoverType = Lookup(BodyCoverTypes, record.BodyCoverType, "Body cover"),
            AppearanceType = Lookup(AppearanceTypes, record.AppearanceType, "Appearance"),
            Diet = Lookup(DietTypes, record.DietType, "Diet"),
            Reproduction = Lookup(ReproductionTypes, record.ReproductionType, "Reproduction"),
            ReproductionMethod = Lookup(ReproductionMethodTypes, record.ReproductionMethodType, "Reproduction method"),
            Religion = Lookup(ReligionTypes, record.ReligionType, "Religion"),
            Devotion = record.Devotion,
            DevotionLevel = record.Devotion switch
            {
                0 => AlienDevotionLevel.None,
                <= 3 => AlienDevotionLevel.Poor,
                <= 6 => AlienDevotionLevel.Fair,
                <= 8 => AlienDevotionLevel.Good,
                _ => AlienDevotionLevel.High
            },
            BiologyProfile = new AlienBiologyProfile
            {
                PsiPower = GetAttribute(record.Attributes, 7),
                PsiRating = GetAttribute(record.Attributes, 7) switch
                {
                    0 => PsiPowerRating.None,
                    <= 2 => PsiPowerRating.VeryPoor,
                    <= 4 => PsiPowerRating.Poor,
                    <= 6 => PsiPowerRating.Fair,
                    <= 8 => PsiPowerRating.Good,
                    _ => PsiPowerRating.Excellent
                },
                Body = GetAttribute(record.Attributes, 9),
                Mind = GetAttribute(record.Attributes, 10),
                Speed = GetAttribute(record.Attributes, 11),
                Lifespan = GetAttribute(record.Attributes, 12)
            },
            MassKg = record.MassKg,
            SizeCm = record.SizeCm,
            LimbPairCount = record.LimbPairCount,
            LegacyAttributes = record.Attributes
        };
    }

    private static Empire CreateEmpire(
        LegacyEmpireImport record,
        IReadOnlyList<LegacyAlienImport> aliens,
        IReadOnlyList<LegacyContactImport> contacts,
        int sectorId)
    {
        var alien = aliens.FirstOrDefault(item => item.LegacyId == record.LegacyId);
        var empire = new Empire
        {
            Id = record.LegacyId,
            Name = string.IsNullOrWhiteSpace(alien?.Name) ? $"Empire {record.LegacyId}" : alien.Name,
            LegacyRaceId = record.LegacyId,
            CivilizationProfile = new CivilizationProfile
            {
                Militancy = GetAttribute(alien?.Attributes, 1),
                Determination = GetAttribute(alien?.Attributes, 2),
                RacialTolerance = GetAttribute(alien?.Attributes, 3),
                Progressiveness = GetAttribute(alien?.Attributes, 4),
                Loyalty = GetAttribute(alien?.Attributes, 5),
                SocialCohesion = GetAttribute(alien?.Attributes, 6),
                TechLevel = GetAttribute(alien?.Attributes, 8),
                Art = GetAttribute(alien?.Attributes, 13),
                Individualism = GetAttribute(alien?.Attributes, 14),
                SpatialAge = GetAttribute(alien?.Attributes, 15)
            },
            EconomicPowerMcr = GetEmpireAttribute(record.Attributes, 1),
            MilitaryPower = GetEmpireAttribute(record.Attributes, 2),
            NativePopulationMillions = GetEmpireAttribute(record.Attributes, 3),
            TradeBonusMcr = GetEmpireAttribute(record.Attributes, 4),
            CaptivePopulationMillions = GetEmpireAttribute(record.Attributes, 5),
            IndependentPopulationMillions = GetEmpireAttribute(record.Attributes, 6),
            SubjectPopulationMillions = GetEmpireAttribute(record.Attributes, 7),
            Planets = GetEmpireAttributeAsInt(record.Attributes, 8),
            CaptivePlanets = GetEmpireAttributeAsInt(record.Attributes, 9),
            SubjugatedPlanets = GetEmpireAttributeAsInt(record.Attributes, 10),
            Moons = GetEmpireAttributeAsInt(record.Attributes, 11),
            SubjugatedMoons = GetEmpireAttributeAsInt(record.Attributes, 12),
            IndependentColonies = GetEmpireAttributeAsInt(record.Attributes, 13),
            Founding = new EmpireFounding
            {
                Origin = EmpireOrigin.NativeHomeworld,
                FoundingRaceId = record.LegacyId,
                FoundingWorldId = alien is null ? null : BuildPlanetWorldId(sectorId, Math.Abs(alien.HomePlanetId))
            }
        };

        empire.RaceMemberships.Add(new EmpireRaceMembership
        {
            EmpireId = empire.Id,
            RaceId = record.LegacyId,
            Role = EmpireRaceRole.Founder,
            PopulationMillions = empire.NativePopulationMillions,
            IsPrimary = true
        });

        foreach (var contact in contacts.Where(item => item.Empire1 == empire.Id || item.Empire2 == empire.Id))
        {
            empire.Contacts.Add(new EmpireContact
            {
                EmpireId = empire.Id,
                OtherEmpireId = contact.Empire1 == empire.Id ? contact.Empire2 : contact.Empire1,
                RelationCode = contact.Relation,
                Relation = Lookup(RelationTypes, contact.Relation, "Relation"),
                Age = contact.Age
            });
        }

        return empire;
    }

    private static Colony CreateColony(LegacyColonyImport record, IReadOnlyList<LegacyAlienImport> aliens, int sectorId)
    {
        var worldKind = record.BodyType == 2 ? WorldKind.Moon : WorldKind.Planet;
        var raceName = GetLegacyRaceName(aliens, record.RaceId);
        var allegianceName = record.AllegianceId == ushort.MaxValue ? "Independent" : GetLegacyEmpireName(aliens, record.AllegianceId);
        var colony = new Colony
        {
            Id = BuildColonyId(sectorId, record.LegacyId),
            WorldId = worldKind == WorldKind.Moon
                ? BuildMoonWorldId(sectorId, record.WorldId)
                : BuildPlanetWorldId(sectorId, record.WorldId),
            WorldKind = worldKind,
            RaceId = record.RaceId,
            ColonistRaceName = raceName,
            AllegianceId = record.AllegianceId,
            AllegianceName = allegianceName,
            PoliticalStatus = GetColonyPoliticalStatus(record),
            ControllingEmpireId = record.AllegianceId == ushort.MaxValue ? null : record.AllegianceId,
            ParentEmpireId = record.RaceId,
            FoundingEmpireId = record.RaceId,
            EncodedPopulation = record.Population,
            EstimatedPopulation = EstimatePopulation(record.Population),
            NativePopulationPercent = record.PopulationComposition,
            ColonyClassCode = record.ColonyClass,
            ColonyClass = Lookup(ColonyClasses, record.ColonyClass, "Colony class"),
            Crime = record.Crime,
            Law = record.Law,
            Stability = record.Stability,
            AgeCenturies = record.Age,
            StarportCode = record.Starport,
            Starport = Lookup(StarportTypes, record.Starport, "Starport"),
            GovernmentTypeCode = record.GovernmentType,
            GovernmentType = Lookup(GovernmentTypes, record.GovernmentType, "Government"),
            GrossWorldProductMcr = record.GrossWorldProductMcr,
            MilitaryPower = record.MilitaryPower,
            ExportResourceCode = record.ExportResource,
            ExportResource = LookupZeroBased(EconomicResourceTypes, record.ExportResource, "Export"),
            ImportResourceCode = record.ImportResource,
            ImportResource = LookupZeroBased(EconomicResourceTypes, record.ImportResource, "Import")
        };

        colony.Demographics.Add(new ColonyDemographic
        {
            ColonyId = colony.Id,
            RaceId = record.RaceId,
            RaceName = raceName,
            PopulationPercent = record.PopulationComposition,
            Population = record.PopulationComposition <= 100
                ? colony.EstimatedPopulation * record.PopulationComposition / 100
                : colony.EstimatedPopulation
        });

        AddFacilities(colony.Facilities, record.FacilityFlags);
        return colony;
    }

    private static World CreatePlanetWorld(
        LegacyPlanetImport record,
        int sectorId,
        int starSystemId,
        int astralBodySequence,
        int orbitIndex)
    {
        var world = new World
        {
            Id = BuildPlanetWorldId(sectorId, record.LegacyId),
            Kind = WorldKind.Planet,
            LegacyPlanetId = record.LegacyId,
            StarSystemId = starSystemId,
            ParentWorldId = null,
            PrimaryAstralBodySequence = astralBodySequence,
            Name = BuildWorldName(record.Name, $"Planet {orbitIndex + 1}"),
            WorldType = Lookup(WorldTypes, record.WorldType, "World"),
            AtmosphereType = Lookup(AtmosphereTypes, record.AtmosphereType, "Atmosphere"),
            AtmosphereComposition = Lookup(AtmosphereCompositions, record.AtmosphereComposition, "Composition"),
            WaterType = Lookup(WaterTypes, record.WaterType, "Water"),
            DiameterKm = record.DiameterKm,
            DensityTenthsEarth = record.DensityTenthsEarth,
            AtmosphericPressure = record.AtmosphericPressure,
            AverageTemperatureCelsius = record.AverageTemperatureCelsius,
            MiscellaneousFlags = record.MiscellaneousFlags,
            AlienRaceId = record.AlienRaceId < 0 ? null : record.AlienRaceId,
            AllegianceId = record.AllegianceId,
            OrbitRadiusAu = record.OrbitRadiusTenthsAu / 10d,
            SmallestMolecularWeightRetained = record.SmallestMolecularWeightRetained,
            AxialTiltDegrees = record.AxialTiltDegrees,
            OrbitalInclinationDegrees = record.OrbitalInclinationDegrees,
            RotationPeriodHours = record.RotationPeriodHours < 0 ? null : (uint)record.RotationPeriodHours,
            EccentricityThousandths = record.EccentricityThousandths,
            Albedo = record.Hydrography[3] / 100d
        };

        ApplyHydrography(world, record.Hydrography);
        ApplyMineralResources(world, record.MineralResources);
        ApplyUnusualCharacteristics(world, record.UnusualFeatureFlags);

        return world;
    }

    private static World CreateMoonWorld(
        LegacyMoonImport record,
        int sectorId,
        int starSystemId,
        int parentWorldId,
        int astralBodySequence,
        int moonIndex)
    {
        var world = new World
        {
            Id = BuildMoonWorldId(sectorId, record.LegacyId),
            Kind = WorldKind.Moon,
            LegacyMoonId = record.LegacyId,
            StarSystemId = starSystemId,
            ParentWorldId = parentWorldId,
            PrimaryAstralBodySequence = astralBodySequence,
            Name = BuildWorldName(record.Name, $"Moon {moonIndex + 1}"),
            WorldType = Lookup(WorldTypes, record.WorldType, "World"),
            AtmosphereType = Lookup(AtmosphereTypes, record.AtmosphereType, "Atmosphere"),
            AtmosphereComposition = Lookup(AtmosphereCompositions, record.AtmosphereComposition, "Composition"),
            WaterType = Lookup(WaterTypes, record.WaterType, "Water"),
            DiameterKm = record.DiameterKm,
            DensityTenthsEarth = record.DensityTenthsEarth,
            AtmosphericPressure = record.AtmosphericPressure,
            AverageTemperatureCelsius = record.AverageTemperatureCelsius,
            MiscellaneousFlags = record.MiscellaneousFlags,
            OrbitRadiusKm = record.OrbitRadiusThousandKm * 1_000d,
            Albedo = record.Hydrography[3] / 100d
        };

        ApplyHydrography(world, record.Hydrography);
        ApplyMineralResources(world, record.MineralResources);

        return world;
    }

    private async Task<int> GetNextSectorIdAsync(CancellationToken cancellationToken)
    {
        var maxSectorId = await dbContext.Sectors
            .Select(sector => (int?)sector.Id)
            .MaxAsync(cancellationToken) ?? 0;

        return maxSectorId + 1;
    }

    private static string NormalizeSectorName(string requestedName)
    {
        return string.IsNullOrWhiteSpace(requestedName) ? "Imported Sector" : requestedName.Trim();
    }

    private static bool IsIndependentColony(Colony colony, IndependenceEventIndex independenceEvents)
    {
        if (colony.PoliticalStatus == ColonyPoliticalStatus.Independent || colony.AllegianceId == ushort.MaxValue)
        {
            return true;
        }

        return independenceEvents.HasIndependenceEvent(colony);
    }

    private static int? FindIndependenceCentury(Colony colony, IndependenceEventIndex independenceEvents)
    {
        return independenceEvents.FindIndependenceCentury(colony);
    }

    private static IndependenceEventIndex BuildIndependenceEventIndex(IEnumerable<HistoryEvent> sectorHistory)
    {
        var index = new IndependenceEventIndex();
        foreach (var history in sectorHistory.Where(LooksLikeIndependenceEvent))
        {
            index.Add(history);
        }

        return index;
    }

    private static bool LooksLikeIndependenceEvent(HistoryEvent history)
    {
        var text = string.Concat(history.EventType, " ", history.Description);
        return text.Contains("independ", StringComparison.OrdinalIgnoreCase)
            || text.Contains("seced", StringComparison.OrdinalIgnoreCase)
            || text.Contains("liberat", StringComparison.OrdinalIgnoreCase)
            || text.Contains("revolt", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class IndependenceEventIndex
    {
        private readonly Dictionary<int, int> colonyCenturies = [];
        private readonly Dictionary<int, int> worldCenturies = [];
        private readonly Dictionary<IndependenceRaceEmpireKey, int> raceEmpireCenturies = [];

        public void Add(HistoryEvent history)
        {
            AddMinimumCentury(colonyCenturies, history.ColonyId, history.Century);
            AddMinimumCentury(worldCenturies, history.PlanetId, history.Century);

            if (history.RaceId is { } raceId && history.EmpireId is { } empireId)
            {
                AddMinimumCentury(
                    raceEmpireCenturies,
                    new IndependenceRaceEmpireKey(raceId, empireId),
                    history.Century);
            }
        }

        public bool HasIndependenceEvent(Colony colony)
        {
            return FindIndependenceCentury(colony) is not null;
        }

        public int? FindIndependenceCentury(Colony colony)
        {
            var bestCentury = GetMinimumCentury(colonyCenturies, colony.Id);
            bestCentury = MinNullable(bestCentury, GetMinimumCentury(worldCenturies, colony.WorldId));

            if (colony.PoliticalStatus == ColonyPoliticalStatus.Independent || colony.AllegianceId == ushort.MaxValue)
            {
                bestCentury = MinNullable(bestCentury, FindRaceEmpireCentury(colony, colony.ParentEmpireId));
                bestCentury = MinNullable(bestCentury, FindRaceEmpireCentury(colony, colony.FoundingEmpireId));
                bestCentury = MinNullable(bestCentury, colony.AllegianceId == ushort.MaxValue
                    ? null
                    : FindRaceEmpireCentury(colony, colony.AllegianceId));
            }

            return bestCentury;
        }

        private int? FindRaceEmpireCentury(Colony colony, int? empireId)
        {
            return empireId is { } id
                ? GetMinimumCentury(raceEmpireCenturies, new IndependenceRaceEmpireKey(colony.RaceId, id))
                : null;
        }

        private static void AddMinimumCentury<TKey>(Dictionary<TKey, int> centuries, TKey? key, int century)
            where TKey : struct
        {
            if (key is not { } value)
            {
                return;
            }

            if (!centuries.TryGetValue(value, out var existingCentury) || century < existingCentury)
            {
                centuries[value] = century;
            }
        }

        private static int? GetMinimumCentury<TKey>(IReadOnlyDictionary<TKey, int> centuries, TKey key)
            where TKey : notnull
        {
            return centuries.TryGetValue(key, out var century)
                ? century
                : null;
        }

        private static int? MinNullable(int? first, int? second)
        {
            return first is null
                ? second
                : second is null
                    ? first
                    : Math.Min(first.Value, second.Value);
        }
    }

    private readonly record struct IndependenceRaceEmpireKey(int RaceId, int EmpireId);

    private static bool HasSpaceHabitatFacility(Colony colony)
    {
        return colony.Facilities.Any(facility =>
            facility.Contains("space habitat", StringComparison.OrdinalIgnoreCase)
            || facility.Contains("orbital habitat", StringComparison.OrdinalIgnoreCase));
    }

    private SpaceHabitat CreateSpaceHabitat(
        Colony colony,
        World world,
        IReadOnlyDictionary<int, Empire> knownEmpires)
    {
        var controllingEmpire = ResolveEmpire(knownEmpires, colony.ControllingEmpireId)
            ?? ResolveEmpire(knownEmpires, colony.AllegianceId == ushort.MaxValue ? null : colony.AllegianceId)
            ?? ResolveEmpire(knownEmpires, colony.ParentEmpireId)
            ?? ResolveEmpire(knownEmpires, colony.FoundingEmpireId)
            ?? ResolveEmpire(knownEmpires, colony.RaceId);
        var name = string.IsNullOrWhiteSpace(world.Name)
            ? "Space Habitat"
            : $"{world.Name} Space Habitat";

        if (controllingEmpire is null)
        {
            return new SpaceHabitat
            {
                Name = name,
                OrbitTargetKind = OrbitTargetKind.World,
                OrbitTargetId = world.Id
            };
        }

        return spaceHabitatConstructionService.BuildOrbitingWorld(controllingEmpire, world, name);
    }

    private static Empire? ResolveEmpire(IReadOnlyDictionary<int, Empire> knownEmpires, int? empireId)
    {
        return empireId is { } id && knownEmpires.TryGetValue(id, out var empire)
            ? empire
            : null;
    }

    private static void AssignColonyToIndependentEmpire(Colony colony, Empire empire)
    {
        colony.ControllingEmpireId = empire.Id;
        colony.FoundingEmpireId = empire.Id;
        colony.PoliticalStatus = ColonyPoliticalStatus.Controlled;
        colony.AllegianceName = empire.Name;
    }

    private static int BuildPlanetWorldId(int sectorId, int legacyPlanetId)
    {
        return sectorId * 1_000_000 + legacyPlanetId + 1;
    }

    private static int BuildMoonWorldId(int sectorId, int legacyMoonId)
    {
        return sectorId * 1_000_000 + 500_000 + legacyMoonId + 1;
    }

    private static int BuildStarSystemId(int sectorId, int legacySystemId)
    {
        return sectorId * 100_000 + legacySystemId + 1;
    }

    private static int BuildColonyId(int sectorId, int legacyColonyId)
    {
        return sectorId * 1_000_000 + 800_000 + legacyColonyId + 1;
    }

    private static string Lookup(IReadOnlyList<string> values, byte legacyCode, string label)
    {
        return legacyCode >= 1 && legacyCode <= values.Count
            ? values[legacyCode - 1]
            : $"{label} {legacyCode}";
    }

    private static string LookupZeroBased(IReadOnlyList<string> values, byte legacyCode, string label)
    {
        return legacyCode < values.Count
            ? values[legacyCode]
            : $"{label} {legacyCode}";
    }

    private static byte GetAttribute(IReadOnlyList<byte>? attributes, int oneBasedIndex)
    {
        return attributes is not null && oneBasedIndex >= 1 && oneBasedIndex <= attributes.Count
            ? attributes[oneBasedIndex - 1]
            : (byte)0;
    }

    private static long GetEmpireAttribute(IReadOnlyList<int> attributes, int oneBasedIndex)
    {
        return oneBasedIndex >= 1 && oneBasedIndex <= attributes.Count
            ? attributes[oneBasedIndex - 1]
            : 0;
    }

    private static int GetEmpireAttributeAsInt(IReadOnlyList<int> attributes, int oneBasedIndex)
    {
        return (int)Math.Clamp(GetEmpireAttribute(attributes, oneBasedIndex), int.MinValue, int.MaxValue);
    }

    private static ColonyPoliticalStatus GetColonyPoliticalStatus(LegacyColonyImport record)
    {
        if (record.AllegianceId == ushort.MaxValue)
        {
            return ColonyPoliticalStatus.Independent;
        }

        if (record.AllegianceId == record.RaceId)
        {
            return ColonyPoliticalStatus.Controlled;
        }

        return ColonyPoliticalStatus.Subject;
    }

    private static long EstimatePopulation(byte encodedPopulation)
    {
        var factor = encodedPopulation / 10;
        var multiplier = encodedPopulation - factor * 10;
        var scale = factor switch
        {
            0 => 10_000L,
            1 => 100_000L,
            2 => 1_000_000L,
            3 => 10_000_000L,
            4 => 100_000_000L,
            5 => 1_000_000_000L,
            _ => 1_000_000_000L
        };

        var adjustedMultiplier = factor >= 6 ? (multiplier + 1) / 10m : multiplier + 1;
        return (long)(adjustedMultiplier * scale);
    }

    private static void AddFacilities(IList<string> facilities, IReadOnlyList<byte> flags)
    {
        for (var byteIndex = 0; byteIndex < flags.Count; byteIndex++)
        {
            for (var bitIndex = 0; bitIndex < 8; bitIndex++)
            {
                if ((flags[byteIndex] & (1 << bitIndex)) == 0)
                {
                    continue;
                }

                var facilityIndex = byteIndex * 8 + bitIndex;
                facilities.Add(facilityIndex < FacilityTypes.Length
                    ? FacilityTypes[facilityIndex]
                    : $"Facility {facilityIndex + 1}");
            }
        }
    }

    private static string GetLegacyRaceName(IReadOnlyList<LegacyAlienImport> aliens, int raceId)
    {
        var race = aliens.FirstOrDefault(item => item.LegacyId == raceId);
        return race is not null && !string.IsNullOrWhiteSpace(race.Name)
            ? race.Name
            : $"Race {raceId}";
    }

    private static string GetLegacyEmpireName(IReadOnlyList<LegacyAlienImport> aliens, int empireId)
    {
        var race = aliens.FirstOrDefault(item => item.LegacyId == empireId);
        return race is not null && !string.IsNullOrWhiteSpace(race.Name)
            ? race.Name
            : $"Empire {empireId}";
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static int? ParseNullableInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static List<string> SplitSemicolonCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ';' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string BuildHistoryKey(HistoryEvent history)
    {
        return string.Concat(
            history.Century,
            "|",
            history.EventType,
            "|",
            history.RaceId,
            "|",
            history.OtherRaceId,
            "|",
            history.EmpireId,
            "|",
            history.ColonyId,
            "|",
            history.PlanetId,
            "|",
            history.StarSystemId,
            "|",
            history.Description);
    }

    private static string BuildWorldName(string legacyName, string fallback)
    {
        return string.IsNullOrWhiteSpace(legacyName) ? fallback : legacyName;
    }

    private static void ApplyHydrography(World world, IReadOnlyList<byte> hydrography)
    {
        if (hydrography.Count < 3)
        {
            return;
        }

        world.Hydrography.WaterPercent = hydrography[0];
        world.Hydrography.IcePercent = hydrography[1];
        world.Hydrography.CloudPercent = hydrography[2];
    }

    private static void ApplyMineralResources(World world, IReadOnlyList<byte> mineralResources)
    {
        if (mineralResources.Count < 5)
        {
            return;
        }

        world.MineralResources.MetalOre = mineralResources[0];
        world.MineralResources.RadioactiveOre = mineralResources[1];
        world.MineralResources.PreciousMetal = mineralResources[2];
        world.MineralResources.RawCrystals = mineralResources[3];
        world.MineralResources.PreciousGems = mineralResources[4];
    }

    private static void ApplyUnusualCharacteristics(World world, IReadOnlyList<byte> unusualFeatureFlags)
    {
        for (var flagIndex = 0; flagIndex < unusualFeatureFlags.Count; flagIndex++)
        {
            for (var bitIndex = 0; bitIndex < 8; bitIndex++)
            {
                var bitValue = 1 << bitIndex;
                if ((unusualFeatureFlags[flagIndex] & bitValue) != bitValue)
                {
                    continue;
                }

                var code = flagIndex * 8 + bitIndex + 1;
                world.UnusualCharacteristics.Add(new UnusualCharacteristic
                {
                    Code = (byte)code,
                    Name = code <= UnusualCharacteristics.Length
                        ? UnusualCharacteristics[code - 1]
                        : $"Unusual {code}"
                });
            }
        }
    }

    private static bool TryReadExact(Stream stream, byte[] buffer)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
            if (read == 0)
            {
                return false;
            }

            totalRead += read;
        }

        return true;
    }

    private static string ReadDelphiShortString(byte[] buffer, int offset, int maxLength)
    {
        var length = Math.Min(buffer[offset], maxLength);
        return System.Text.Encoding.Latin1.GetString(buffer, offset + 1, length).TrimEnd('\0', ' ');
    }

    private static IReadOnlyList<string> BuildSpectralClasses(byte[] buffer, byte starCount)
    {
        var samples = new List<string>();
        var count = Math.Min(starCount, (byte)3);
        for (var index = 0; index < count; index++)
        {
            samples.Add($"{buffer[index]}-{buffer[3 + index]}");
        }

        return samples;
    }

    private sealed record LegacyStarSystemImport(
        int LegacyId,
        StarSystem System,
        byte[] PlanetCounts,
        int[] FirstPlanetIds);

    private sealed record LegacyPlanetImport(
        int LegacyId,
        int StarSystemId,
        int AlienRaceId,
        int FirstMoonId,
        ushort AllegianceId,
        byte AtmosphereType,
        byte WorldType,
        byte WaterType,
        byte AtmosphereComposition,
        byte SmallestMolecularWeightRetained,
        byte AxialTiltDegrees,
        byte OrbitalInclinationDegrees,
        short AverageTemperatureCelsius,
        byte SatelliteCount,
        byte[] Hydrography,
        int RotationPeriodHours,
        int DiameterKm,
        byte DensityTenthsEarth,
        float AtmosphericPressure,
        ushort EccentricityThousandths,
        short OrbitRadiusTenthsAu,
        byte[] MineralResources,
        byte MiscellaneousFlags,
        byte[] UnusualFeatureFlags,
        string Name);

    private sealed record LegacyMoonImport(
        int LegacyId,
        int PlanetId,
        byte AtmosphereType,
        byte WorldType,
        byte WaterType,
        byte AtmosphereComposition,
        short AverageTemperatureCelsius,
        byte[] Hydrography,
        int DiameterKm,
        byte DensityTenthsEarth,
        float AtmosphericPressure,
        ushort OrbitRadiusThousandKm,
        byte[] MineralResources,
        byte MiscellaneousFlags,
        string Name);

    private sealed record LegacyAlienImport(
        int LegacyId,
        int HomePlanetId,
        byte EnvironmentType,
        byte BodyType,
        byte LimbPairCount,
        byte DietType,
        byte ReproductionType,
        byte ReproductionMethodType,
        byte GovernmentType,
        byte BodyCoverType,
        byte AppearanceType,
        short MassKg,
        short SizeCm,
        byte[] LimbTypes,
        byte[] Attributes,
        byte[] AbilityFlags,
        byte[] ColorFlags,
        byte[] HairColorFlags,
        byte[] BodyCharacteristicFlags,
        byte[] EyeColorFlags,
        byte[] EyeCharacteristicFlags,
        byte HairType,
        byte ReligionType,
        byte Devotion,
        string Name);

    private sealed record LegacyColonyImport(
        int LegacyId,
        int WorldId,
        ushort RaceId,
        ushort AllegianceId,
        byte BodyType,
        byte Population,
        byte ColonyClass,
        byte Crime,
        byte Law,
        byte Stability,
        byte Age,
        byte Starport,
        byte GovernmentType,
        ushort GrossWorldProductMcr,
        ushort MilitaryPower,
        byte PopulationComposition,
        byte[] FacilityFlags,
        byte ExportResource,
        byte ImportResource);

    private sealed record LegacyEmpireImport(
        int LegacyId,
        int[] Attributes);

    private sealed record LegacyContactImport(
        int LegacyId,
        ushort Empire1,
        ushort Empire2,
        byte Relation,
        byte Age);
}
