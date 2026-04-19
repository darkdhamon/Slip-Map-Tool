using System.IO.Compression;
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
    private readonly StarWinClassificationCatalog classificationCatalog = new();

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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(zipPackage);

        await using var bufferedPackage = new MemoryStream();
        await zipPackage.CopyToAsync(bufferedPackage, cancellationToken);

        bufferedPackage.Position = 0;
        var preview = await PreviewStarWinZipAsync(bufferedPackage, packageName, cancellationToken);
        if (!preview.CanImport)
        {
            return new StarWinLegacyImportResult(false, null, preview, ["The package cannot be imported until required files are present."]);
        }

        bufferedPackage.Position = 0;
        using var archive = new ZipArchive(bufferedPackage, ZipArchiveMode.Read, leaveOpen: true);
        var sectorPreview = preview.Sectors[0];
        var sunFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".sun", StringComparison.OrdinalIgnoreCase));
        var planetFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".pln", StringComparison.OrdinalIgnoreCase));
        var moonFile = sectorPreview.Files.First(file => string.Equals(file.Extension, ".mon", StringComparison.OrdinalIgnoreCase));
        var sunEntry = archive.GetEntry(sunFile.EntryName);
        var planetEntry = archive.GetEntry(planetFile.EntryName);
        var moonEntry = archive.GetEntry(moonFile.EntryName);
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

        var requestedSectorName = NormalizeSectorName(
            string.IsNullOrWhiteSpace(targetSectorName) ? sectorPreview.SectorName : targetSectorName);
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

        var importedSystems = ReadStarSystems(sunEntry, sectorId, sectorPreview.StarSystemRecordCount)
            .ToList();
        var planets = ReadPlanetRecords(planetEntry).ToList();
        var moons = ReadMoonRecords(moonEntry).ToList();
        var addedSystemCount = 0;
        var addedAstralBodyCount = 0;
        var addedWorldCount = 0;
        var addedMoonCount = 0;
        var skippedSystemCount = 0;
        var skippedWorldCount = 0;

        foreach (var systemImport in importedSystems)
        {
            var worlds = BuildWorlds(systemImport, planets, moons, sectorId);
            var newWorlds = worlds
                .Where(world => !existingWorldIds.Contains(world.Id))
                .ToList();
            skippedWorldCount += worlds.Count - newWorlds.Count;
            foreach (var world in newWorlds)
            {
                existingWorldIds.Add(world.Id);
            }

            if (existingSystemIds.Add(systemImport.System.Id))
            {
                foreach (var world in newWorlds)
                {
                    systemImport.System.Worlds.Add(world);
                }

                sector.Systems.Add(systemImport.System);
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

        if (addedSystemCount > 0 || addedAstralBodyCount > 0 || addedWorldCount > 0)
        {
            sector.History.Add(new HistoryEvent
            {
                SectorId = sector.Id,
                Century = 0,
                EventType = "Legacy Import",
                Description = $"Merged {addedSystemCount:N0} missing star systems, {addedAstralBodyCount:N0} missing astral bodies, and {addedWorldCount:N0} missing worlds from {packageName}. Existing records were not overwritten."
            });
        }

        if (isNewSector)
        {
            dbContext.Sectors.Add(sector);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new StarWinLegacyImportResult(
            true,
            sector.Id,
            preview,
            [
                $"{sector.Name} merged from {packageName}.",
                $"Added {addedSystemCount:N0} star systems, {addedAstralBodyCount:N0} astral bodies, {addedWorldCount:N0} worlds, and {addedMoonCount:N0} moon satellites.",
                $"Skipped {skippedSystemCount:N0} existing star systems and {skippedWorldCount:N0} existing worlds without overwriting."
            ]);
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

    private static int BuildPlanetWorldId(int sectorId, int legacyPlanetId)
    {
        return sectorId * 1_000_000 + legacyPlanetId + 1;
    }

    private static int BuildMoonWorldId(int sectorId, int legacyMoonId)
    {
        return sectorId * 1_000_000 + 500_000 + legacyMoonId + 1;
    }

    private static string Lookup(IReadOnlyList<string> values, byte legacyCode, string label)
    {
        return legacyCode >= 1 && legacyCode <= values.Count
            ? values[legacyCode - 1]
            : $"{label} {legacyCode}";
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
}
