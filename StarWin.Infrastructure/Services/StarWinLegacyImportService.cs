using System.IO.Compression;
using System.Globalization;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StarWin.Application.Services.LegacyImport;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinLegacyImportService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinLegacyImportService
{
    private const int StarRecordSize = 196;
    private const int PlanetRecordSize = 84;
    private const int MoonRecordSize = 60;
    private const int AlienRecordSize = 92;
    private const int ColonyRecordSize = 28;
    private const int ContactRecordSize = 6;
    private const int EmpireRecordSize = 52;
    private const int StarSystemSaveBatchSize = 500;
    private const int WorldSaveBatchSize = 25_000;
    private const int SqlBulkCopyBatchSize = 10_000;
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

    private static readonly string[] StarSystemColumns =
    [
        "Id",
        "SectorId",
        "LegacySystemId",
        "Name",
        "Coordinates",
        "AllegianceId",
        "MapCode"
    ];

    private static readonly string[] AstralBodyColumns =
    [
        "Role",
        "Kind",
        "Classification",
        "ClassificationCode",
        "DecimalClassCode",
        "PlanetCount",
        "Luminosity",
        "SolarMasses",
        "CompanionOrbitAu",
        "StarSystemId"
    ];

    private static readonly string[] WorldColumns =
    [
        "Id",
        "Kind",
        "LegacyPlanetId",
        "LegacyMoonId",
        "StarSystemId",
        "ParentWorldId",
        "PrimaryAstralBodySequence",
        "Name",
        "WorldType",
        "AtmosphereType",
        "AtmosphereComposition",
        "WaterType",
        "Hydrography_WaterPercent",
        "Hydrography_IcePercent",
        "Hydrography_CloudPercent",
        "MineralResources_MetalOre",
        "MineralResources_RadioactiveOre",
        "MineralResources_PreciousMetal",
        "MineralResources_RawCrystals",
        "MineralResources_PreciousGems",
        "DiameterKm",
        "DensityTenthsEarth",
        "AtmosphericPressure",
        "AverageTemperatureCelsius",
        "MiscellaneousFlags",
        "Albedo",
        "AlienRaceId",
        "ControlledByEmpireId",
        "AllegianceId",
        "OrbitRadiusAu",
        "OrbitRadiusKm",
        "SmallestMolecularWeightRetained",
        "AxialTiltDegrees",
        "OrbitalInclinationDegrees",
        "RotationPeriodHours",
        "EccentricityThousandths",
        "OrbitPeriodDays",
        "GravityEarthG",
        "MassEarthMasses",
        "EscapeVelocityKmPerSecond",
        "OxygenPressureAtmospheres",
        "BoilingPointCelsius",
        "MagneticFieldGauss"
    ];

    private static readonly string[] UnusualCharacteristicColumns =
    [
        "Code",
        "Name",
        "Notes",
        "WorldId"
    ];

    private static readonly string[] ColonyColumns =
    [
        "Id",
        "WorldId",
        "Name",
        "WorldKind",
        "RaceId",
        "ColonistRaceName",
        "AllegianceId",
        "AllegianceName",
        "PoliticalStatus",
        "ControllingEmpireId",
        "ParentEmpireId",
        "FoundingEmpireId",
        "EncodedPopulation",
        "EstimatedPopulation",
        "NativePopulationPercent",
        "ColonyClass",
        "ColonyClassCode",
        "Crime",
        "Law",
        "Stability",
        "AgeCenturies",
        "Starport",
        "StarportCode",
        "GovernmentType",
        "GovernmentTypeCode",
        "GrossWorldProductMcr",
        "MilitaryPower",
        "ExportResource",
        "ExportResourceCode",
        "ImportResource",
        "ImportResourceCode"
    ];

    private static readonly string[] ColonyDemographicColumns =
    [
        "ColonyId",
        "RaceId",
        "RaceName",
        "PopulationPercent",
        "Population"
    ];

    private static readonly string[] HistoryEventColumns =
    [
        "SectorId",
        "Century",
        "EventType",
        "RaceId",
        "OtherRaceId",
        "EmpireId",
        "ColonyId",
        "PlanetId",
        "StarSystemId",
        "Description",
        "ImportDataJson"
    ];

    private static readonly string[] AlienRaceColumns =
    [
        "Id",
        "HomePlanetId",
        "Name",
        "EnvironmentType",
        "BodyChemistry",
        "BodyCoverType",
        "AppearanceType",
        "Diet",
        "Reproduction",
        "ReproductionMethod",
        "Devotion",
        "DevotionLevel",
        "CivilizationProfile_Militancy",
        "CivilizationProfile_Determination",
        "CivilizationProfile_RacialTolerance",
        "CivilizationProfile_Progressiveness",
        "CivilizationProfile_Loyalty",
        "CivilizationProfile_SocialCohesion",
        "CivilizationProfile_Art",
        "CivilizationProfile_Individualism",
        "BiologyProfile_PsiPower",
        "BiologyProfile_PsiRating",
        "BiologyProfile_Body",
        "BiologyProfile_Mind",
        "BiologyProfile_Speed",
        "BiologyProfile_Lifespan",
        "GravityPreference",
        "TemperaturePreference",
        "AtmosphereBreathed",
        "MassKg",
        "SizeCm",
        "LimbPairCount",
        "LimbTypes",
        "Abilities",
        "BodyCharacteristics",
        "EyeCharacteristics",
        "EyeColors",
        "HairColors",
        "HairType",
        "Colors",
        "ColorPattern",
        "LegacyAttributes",
        "RequiresUserRename",
        "ImportDataJson"
    ];

    private static readonly string[] EmpireColumns =
    [
        "Id",
        "Name",
        "LegacyRaceId",
        "GovernmentType",
        "IsFallen",
        "CivilizationProfile_Militancy",
        "CivilizationProfile_Determination",
        "CivilizationProfile_RacialTolerance",
        "CivilizationProfile_Progressiveness",
        "CivilizationProfile_Loyalty",
        "CivilizationProfile_SocialCohesion",
        "CivilizationProfile_TechLevel",
        "CivilizationProfile_Art",
        "CivilizationProfile_Individualism",
        "CivilizationProfile_SpatialAge",
        "CivilizationModifiers_Militancy",
        "CivilizationModifiers_Determination",
        "CivilizationModifiers_RacialTolerance",
        "CivilizationModifiers_Progressiveness",
        "CivilizationModifiers_Loyalty",
        "CivilizationModifiers_SocialCohesion",
        "CivilizationModifiers_Art",
        "CivilizationModifiers_Individualism",
        "Founding_Origin",
        "Founding_FoundingWorldId",
        "Founding_FoundingColonyId",
        "Founding_ParentEmpireId",
        "Founding_FoundingRaceId",
        "Founding_FoundedCentury",
        "ExpansionPolicy",
        "EconomicPowerMcr",
        "MilitaryPower",
        "TradeBonusMcr",
        "Planets",
        "CaptivePlanets",
        "Moons",
        "SubjugatedPlanets",
        "SubjugatedMoons",
        "IndependentColonies",
        "SpaceHabitats",
        "NativePopulationMillions",
        "CaptivePopulationMillions",
        "SubjectPopulationMillions",
        "IndependentPopulationMillions",
        "MilitaryForces_Personnel_CrewRating",
        "MilitaryForces_Personnel_CrewQuality",
        "MilitaryForces_Personnel_TroopRating",
        "MilitaryForces_Personnel_TroopQuality",
        "MilitaryForces_Personnel_ConscriptionPolicy",
        "MilitaryForces_NavyDoctrine_FighterEmphasisPercent",
        "MilitaryForces_NavyDoctrine_MissileEmphasisPercent",
        "MilitaryForces_NavyDoctrine_BeamWeaponEmphasisPercent",
        "MilitaryForces_NavyDoctrine_AssaultEmphasisPercent",
        "MilitaryForces_NavyDoctrine_DefenseEmphasisPercent",
        "MilitaryForces_Notes",
        "ImportDataJson"
    ];

    private static readonly string[] EmpireRaceMembershipColumns =
    [
        "EmpireId",
        "RaceId",
        "Role",
        "PopulationMillions",
        "IsPrimary"
    ];

    private static readonly string[] EmpireContactColumns =
    [
        "EmpireId",
        "OtherEmpireId",
        "Relation",
        "RelationCode",
        "Age"
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

    private static readonly string[] ColorTypes =
    [
        "White",
        "Black",
        "Yellow",
        "Red",
        "Gray",
        "Blue",
        "Green",
        "Brown",
        "Pink",
        "Orange",
        "Crimson",
        "Violet",
        "Clear",
        "Calico",
        "Silver",
        "Gold"
    ];

    private static readonly string[] HairColorTypes =
    [
        "White",
        "Black",
        "Blonde",
        "Red",
        "Gray",
        "Blue",
        "Green",
        "Chestnut",
        "Pink",
        "Orange",
        "Crimson",
        "Violet",
        "Clear",
        "Calico",
        "Silver",
        "Gold"
    ];

    private static readonly string[] EyeColorTypes =
    [
        "White",
        "Black",
        "Yellow",
        "Red",
        "Gray",
        "Blue",
        "Green",
        "Hazel",
        "Pink",
        "Orange",
        "Crimson",
        "Violet",
        "Clear",
        "Calico",
        "Silver",
        "Gold"
    ];

    private static readonly string[] BodyPartTypes =
    [
        "Tail",
        "Trunk",
        "Horn(s)",
        "Antennas",
        "Fangs",
        "Claws",
        "Nippers",
        "Hooves",
        "Jelly bag",
        "bp10",
        "One color",
        "Two-tone",
        "Multi color",
        "Striped/Banded",
        "Spots",
        "Randomly mottled"
    ];

    private static readonly string[] EyeDetailTypes =
    [
        "No eyes",
        "Single",
        "Three",
        "Four",
        "Multiple",
        "Large",
        "Double-lidded",
        "Bulging",
        "Luminous",
        "Stalked",
        "Round pupil",
        "Slitted pupil",
        "No pupil",
        "Multifaceted"
    ];

    private static readonly string[] HairTypes =
    [
        "No hair",
        "Rare",
        "Bony crest",
        "Palps",
        "Crest",
        "Fur",
        "Feather",
        "Short"
    ];

    private static readonly string[] LimbRoleTypes =
    [
        "None",
        "Wings",
        "Fins",
        "Legs",
        "Dual-purpose arm/legs",
        "Arms",
        "Tentacles",
        "Pseudopods"
    ];

    private static readonly string[] SpecialAbilityTypes =
    [
        "Acute hearing",
        "Poor hearing",
        "Acute smell",
        "Acute vision",
        "Poor vision",
        "Ambidextrous",
        "Chameleon skin",
        "Cold sensitivity",
        "Cold tolerance",
        "Color blind",
        "Heat sensitivity",
        "Heat tolerance",
        "Infrared vision",
        "Night vision",
        "Poison",
        "Radiation tolerance",
        "Fast healing",
        "Sonar",
        "Wall climbing",
        "Web spinning",
        "Nictating membrane",
        "Radio hearing",
        "Acid secretion",
        "Metamorphosis",
        "Electric blast",
        "Hypnotism",
        "Mimicry",
        "Dampen",
        "360 degrees vision",
        "Sonic beam",
        "Vampirism",
        "Slow motion",
        "Sealed system",
        "Clone",
        "Stretching",
        "Systemic antidote",
        "Mystical power",
        "Independent eyes",
        "Quick maturity",
        "Infertile",
        "Hive mind",
        "Bicephalous",
        "Regeneration",
        "Racial memory",
        "Universal digestion",
        "Pressure support",
        "Polarized eyes",
        "Vulnerability to disease",
        "Cultural adaptability",
        "Field sense",
        "Cold blooded biology",
        "Winged flight",
        "Water breathing",
        "Flight",
        "Charisma",
        "Spectrum vision",
        "Ultrasonic hearing",
        "Microscopic vision",
        "Blind",
        "Deafness",
        "Odious racial habit",
        "Merchant bonus skill",
        "Engineer bonus skill",
        "Pilot bonus skill",
        "Combat bonus skill",
        "Science bonus skill",
        "Water dependency",
        "Light sensitivity",
        "Involuntary dampen",
        "Sound sensitivity",
        "Disease tolerance",
        "Eidetic memory",
        "Language talent",
        "No sense of smell/taste",
        "Strange appearance",
        "Manual dexterity",
        "Perfect balance",
        "Foul odor",
        "Skin color change",
        "Dependency",
        "High fecundity",
        "Cybernetic enhancements",
        "Computer skill bonus",
        "Leap",
        "Vibration sense",
        "Toughness",
        "High gravity sensitivity",
        "Toxin intolerance",
        "Extra heart",
        "Heavy sleeper",
        "Light sleeper",
        "Chemical communication",
        "Lightning calculator",
        "Time sense",
        "EM imaging",
        "Ultrasonic communication"
    ];

    private static readonly string[] UniversalSpeciesPrefixes =
    [
        "Neo",
        "Proto",
        "High",
        "Deep",
        "Prime",
        "Star",
        "Void",
        "Astra",
        "Solar",
        "True"
    ];

    private static readonly string[] UniversalSpeciesSuffixes =
    [
        "kin",
        "born",
        "ari",
        "ori",
        "an",
        "ite",
        "id",
        "folk",
        "ward",
        "ae"
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
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

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
                Id = await GetNextSectorIdAsync(dbContext, cancellationToken),
                Name = requestedSectorName
            };
            sector.Configuration.SectorId = sector.Id;
        }

        var sectorId = sector.Id;
        await ReportImportProgressAsync(progress, 24, "Reading existing records...", "Loading existing systems, worlds, races, empires, colonies, and history keys.");
        var existingSystems = isNewSector
            ? []
            : await dbContext.StarSystems
                .Where(system => system.SectorId == sectorId)
                .Include(system => system.AstralBodies)
                .ToListAsync(cancellationToken);
        var existingSystemsById = existingSystems.ToDictionary(system => system.Id);
        var existingSystemIds = existingSystemsById.Keys.ToHashSet();
        var existingWorlds = isNewSector
            ? []
            : await dbContext.Worlds
                .Where(world => world.StarSystemId != null
                    && dbContext.StarSystems.Any(system => system.Id == world.StarSystemId && system.SectorId == sectorId))
                .Include(world => world.UnusualCharacteristics)
                .ToListAsync(cancellationToken);
        var existingWorldsById = existingWorlds.ToDictionary(world => world.Id);
        var existingWorldIds = existingWorldsById.Keys.ToHashSet();
        var existingAstralBodyRoles = existingSystems.ToDictionary(
            system => system.Id,
            system => system.AstralBodies.Select(body => body.Role).ToHashSet());
        var knownAlienRaces = await dbContext.AlienRaces
            .ToDictionaryAsync(race => race.Id, cancellationToken);
        var existingRaceIds = knownAlienRaces.Keys.ToHashSet();
        var knownEmpires = await dbContext.Empires
            .Include(empire => empire.Contacts)
            .Include(empire => empire.RaceMemberships)
            .ToDictionaryAsync(empire => empire.Id, cancellationToken);
        var existingEmpireIds = knownEmpires.Keys.ToHashSet();
        var independentEmpireColonyIds = await dbContext.Empires
            .Where(empire => empire.Founding.Origin == EmpireOrigin.IndependentColony
                && empire.Founding.FoundingColonyId != null)
            .Select(empire => empire.Founding.FoundingColonyId!.Value)
            .ToHashSetAsync(cancellationToken);
        var existingSpaceHabitatWorldIds = await dbContext.SpaceHabitats
            .Where(habitat => habitat.OrbitTargetKind == OrbitTargetKind.World)
            .Select(habitat => habitat.OrbitTargetId)
            .ToHashSetAsync(cancellationToken);
        var existingColonies = await dbContext.Colonies
            .Include(colony => colony.Demographics)
            .ToListAsync(cancellationToken);
        var existingColoniesByWorldId = existingColonies.ToDictionary(colony => colony.WorldId);
        var existingColonyWorldIds = existingColoniesByWorldId.Keys.ToHashSet();
        var existingHistoryKeys = new HashSet<string>(StringComparer.Ordinal);
        var existingHistoryByKey = new Dictionary<string, HistoryEvent>(StringComparer.Ordinal);
        if (!isNewSector)
        {
            var existingHistory = await dbContext.HistoryEvents
                .Where(history => history.SectorId == sectorId)
                .ToListAsync(cancellationToken);
            foreach (var history in existingHistory)
            {
                var historyKey = BuildHistoryKey(history);
                existingHistoryKeys.Add(historyKey);
                existingHistoryByKey[historyKey] = history;
            }
        }

        await ReportImportProgressAsync(progress, 32, "Reading legacy records...", "Parsing StarWin systems, planets, moons, races, empires, colonies, contacts, and history.");
        var importedSystems = ReadStarSystems(sunEntry, sectorId, sectorPreview.StarSystemRecordCount)
            .ToList();
        StarSystemNameUniqueness.EnsureUniqueImportedNames(importedSystems.Select(systemImport => systemImport.System).ToList());
        var planets = ReadPlanetRecords(planetEntry).ToList();
        var moons = ReadMoonRecords(moonEntry).ToList();
        var aliens = ReadAlienRecords(alienEntry).ToList();
        var empires = ReadEmpireRecords(empireEntry).ToList();
        var colonies = ReadColonyRecords(colonyEntry).ToList();
        var contacts = ReadContactRecords(contactEntry).ToList();
        var historyEvents = ReadHistoryEvents(historyEntry, sectorId, historyCsvFile is not null).ToList();
        var legacyAliensById = aliens.ToDictionary(alien => alien.LegacyId);
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
        var mergedSystemCount = 0;
        var mergedWorldCount = 0;
        var mergedAlienCount = 0;
        var mergedEmpireCount = 0;
        var mergedColonyCount = 0;
        var mergedHistoryCount = 0;
        var originalAutoDetectChanges = dbContext.ChangeTracker.AutoDetectChangesEnabled;
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        await using var importTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (isNewSector)
            {
                dbContext.Sectors.Add(sector);
                await FlushImportChangesAsync(
                    dbContext,
                    progress,
                    41,
                    "Saving sector shell...",
                    $"Creating sector {sector.Name} before bulk importing star map records.",
                    cancellationToken);
            }

            await ReportImportProgressAsync(progress, 42, "Writing star systems and worlds...", $"Preparing {importedSystems.Count:N0} star system record(s), {planets.Count:N0} planet record(s), and {moons.Count:N0} moon record(s).");
            var processedSystemCount = 0;
            var batch = new StarMapImportBatch();
            foreach (var systemImport in importedSystems)
            {
                processedSystemCount++;
                if (processedSystemCount == 1 || processedSystemCount % 250 == 0 || processedSystemCount == importedSystems.Count)
                {
                    var systemPercent = CalculatePhasePercent(42, 58, processedSystemCount, importedSystems.Count);
                    await ReportImportProgressAsync(progress, systemPercent, "Writing star systems and worlds...", $"Processed {processedSystemCount:N0} of {importedSystems.Count:N0} star system record(s).");
                }

                var worlds = BuildWorlds(systemImport, planets, moons, sectorId);
                var newWorlds = new List<World>();
                foreach (var world in worlds)
                {
                    if (existingWorldsById.TryGetValue(world.Id, out var existingWorld))
                    {
                        skippedWorldCount++;
                        if (MergeMissingWorldData(existingWorld, world))
                        {
                            mergedWorldCount++;
                        }

                        continue;
                    }

                    newWorlds.Add(world);
                    existingWorldIds.Add(world.Id);
                    existingWorldsById[world.Id] = world;
                }

                if (existingSystemsById.TryGetValue(systemImport.System.Id, out var existingSystem))
                {
                    skippedSystemCount++;
                    var mergedSystem = MergeMissingStarSystemData(existingSystem, systemImport.System);
                    if (!existingAstralBodyRoles.TryGetValue(systemImport.System.Id, out var existingRoles))
                    {
                        existingRoles = [];
                        existingAstralBodyRoles[systemImport.System.Id] = existingRoles;
                    }

                    foreach (var astralBody in systemImport.System.AstralBodies.Where(body => existingRoles.Add(body.Role)))
                    {
                        batch.AstralBodies.Add(new AstralBodyImportRow(systemImport.System.Id, astralBody));
                        addedAstralBodyCount++;
                        mergedSystem = true;
                    }

                    foreach (var world in newWorlds)
                    {
                        batch.Worlds.Add(world);
                        AddUnusualCharacteristicRows(batch.UnusualCharacteristics, world);
                    }

                    if (mergedSystem)
                    {
                        mergedSystemCount++;
                    }

                    addedWorldCount += newWorlds.Count;
                    addedMoonCount += newWorlds.Count(world => world.Kind == WorldKind.Moon);
                }
                else
                {
                    existingSystemIds.Add(systemImport.System.Id);
                    existingSystemsById[systemImport.System.Id] = systemImport.System;
                    foreach (var world in newWorlds)
                    {
                        batch.Worlds.Add(world);
                        AddUnusualCharacteristicRows(batch.UnusualCharacteristics, world);
                    }

                    batch.StarSystems.Add(systemImport.System);
                    AddAstralBodyRows(batch.AstralBodies, systemImport.System.Id, systemImport.System.AstralBodies);
                    addedSystemCount++;
                    addedAstralBodyCount += systemImport.System.AstralBodies.Count;
                    addedWorldCount += newWorlds.Count;
                    addedMoonCount += newWorlds.Count(world => world.Kind == WorldKind.Moon);
                }

                if (batch.HasChanges
                    && (batch.StarSystems.Count >= StarSystemSaveBatchSize
                        || batch.Worlds.Count >= WorldSaveBatchSize
                        || processedSystemCount == importedSystems.Count))
                {
                    var savePercent = CalculatePhasePercent(43, 59, processedSystemCount, importedSystems.Count);
                    await BulkInsertStarMapBatchAsync(
                        dbContext,
                        progress,
                        savePercent,
                        $"Committing batch {processedSystemCount:N0} of {importedSystems.Count:N0}: {batch.StarSystems.Count:N0} star system(s), {batch.AstralBodies.Count:N0} astral bodies, {batch.Worlds.Count:N0} world(s), and {batch.UnusualCharacteristics.Count:N0} unusual characteristic(s).",
                        batch,
                        cancellationToken);
                    batch = new StarMapImportBatch();
                }
            }

            if (batch.HasChanges)
            {
                await BulkInsertStarMapBatchAsync(
                    dbContext,
                    progress,
                    59,
                    $"Committing final batch: {batch.StarSystems.Count:N0} star system(s), {batch.AstralBodies.Count:N0} astral bodies, {batch.Worlds.Count:N0} world(s), and {batch.UnusualCharacteristics.Count:N0} unusual characteristic(s).",
                    batch,
                    cancellationToken);
            }
            else
            {
                await ReportImportProgressAsync(progress, 59, "Saving star systems and worlds...", $"Committed {addedSystemCount:N0} star system(s), {addedAstralBodyCount:N0} astral bodies, and {addedWorldCount:N0} world(s).");
            }

            var importedAlienRaces = new List<AlienRace>();
            var usedRaceNames = knownAlienRaces.Values
                .Select(race => race.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            await ReportImportProgressAsync(progress, 60, "Writing alien races...", $"Preparing {aliens.Count:N0} alien race record(s).");
            foreach (var alienRecord in aliens)
            {
                var alienRace = CreateAlienRace(alienRecord, sectorId, existingWorldsById, usedRaceNames);
                if (knownAlienRaces.TryGetValue(alienRecord.LegacyId, out var existingAlienRace))
                {
                    skippedAlienCount++;
                    if (MergeMissingAlienRaceData(existingAlienRace, alienRace))
                    {
                        mergedAlienCount++;
                    }

                    continue;
                }

                existingRaceIds.Add(alienRecord.LegacyId);
                importedAlienRaces.Add(alienRace);
                knownAlienRaces[alienRace.Id] = alienRace;
                addedAlienCount++;
            }

            var importedEmpires = new List<Empire>();
            var usedEmpireNames = knownEmpires.Values
                .Select(empire => empire.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var addedRaceMembershipCount = 0;
            await ReportImportProgressAsync(progress, 66, "Writing empires and contacts...", $"Preparing {empires.Count:N0} empire record(s) and {contacts.Count:N0} contact record(s).");
            foreach (var empireRecord in empires)
            {
                var empire = CreateEmpire(empireRecord, legacyAliensById, knownAlienRaces, contacts, sectorId, usedEmpireNames);
                if (knownEmpires.TryGetValue(empireRecord.LegacyId, out var existingEmpire))
                {
                    skippedEmpireCount++;
                    if (MergeMissingEmpireData(existingEmpire, empire))
                    {
                        mergedEmpireCount++;
                    }

                    continue;
                }

                existingEmpireIds.Add(empireRecord.LegacyId);
                importedEmpires.Add(empire);
                knownEmpires[empire.Id] = empire;
                addedEmpireCount++;
                addedRaceMembershipCount += empire.RaceMemberships.Count;
                addedContactCount += empire.Contacts.Count;
            }

            await BulkInsertCivilizationReferenceBatchAsync(
                dbContext,
                progress,
                70,
                $"Committing {addedAlienCount:N0} alien race(s), {addedEmpireCount:N0} empire(s), {addedRaceMembershipCount:N0} race membership row(s), and {addedContactCount:N0} contact(s).",
                importedAlienRaces,
                importedEmpires,
                cancellationToken);

            await FlushImportChangesAsync(
                dbContext,
                progress,
                71,
                "Saving races, empires, and contacts...",
                $"Finalizing merged updates for existing races and empires after bulk insert.",
                cancellationToken,
                clearChangeTracker: false);

            if (importedEmpires.Count > 0)
            {
                var existingReligions = await dbContext.Religions
                    .ToDictionaryAsync(religion => religion.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

                foreach (var empire in importedEmpires.Where(item => item.Religions.Count > 0))
                {
                    foreach (var empireReligion in empire.Religions)
                    {
                        if (!existingReligions.TryGetValue(empireReligion.ReligionName, out var religion))
                        {
                            religion = new Religion
                            {
                                Name = empireReligion.ReligionName,
                                Type = InferReligionTypeFromName(empireReligion.ReligionName),
                                IsUserDefined = false
                            };
                            dbContext.Religions.Add(religion);
                            existingReligions[religion.Name] = religion;
                        }
                    }
                }

                await FlushImportChangesAsync(
                    dbContext,
                    progress,
                    72,
                    "Saving religions...",
                    $"Persisting religion definitions and empire religion links for imported empires.",
                    cancellationToken,
                    clearChangeTracker: false);

                foreach (var empire in importedEmpires.Where(item => item.Religions.Count > 0))
                {
                    foreach (var empireReligion in empire.Religions)
                    {
                        var religion = existingReligions[empireReligion.ReligionName];
                        dbContext.Set<EmpireReligion>().Add(new EmpireReligion
                        {
                            EmpireId = empire.Id,
                            ReligionId = religion.Id,
                            ReligionName = empireReligion.ReligionName,
                            PopulationPercent = empireReligion.PopulationPercent
                        });
                    }
                }

                await FlushImportChangesAsync(
                    dbContext,
                    progress,
                    72,
                    "Saving religions...",
                    $"Persisting empire religion links for imported empires.",
                    cancellationToken,
                    clearChangeTracker: false);
            }

            var importedColonies = new List<Colony>();
            var importedHistoryEvents = new List<HistoryEvent>();
            await ReportImportProgressAsync(progress, 73, "Writing colonies...", $"Preparing {colonies.Count:N0} colony record(s).");
            for (var colonyIndex = 0; colonyIndex < colonies.Count; colonyIndex++)
            {
                var colonyRecord = colonies[colonyIndex];
                var colony = CreateColony(colonyRecord, aliens, sectorId);
                if (!existingWorldIds.Contains(colony.WorldId))
                {
                    skippedColonyCount++;
                    continue;
                }

                if (existingColoniesByWorldId.TryGetValue(colony.WorldId, out var existingColony))
                {
                    skippedColonyCount++;
                    if (MergeMissingColonyData(existingColony, colony))
                    {
                        mergedColonyCount++;
                    }

                    continue;
                }

                importedColonies.Add(colony);
                existingColonyWorldIds.Add(colony.WorldId);
                existingColoniesByWorldId[colony.WorldId] = colony;
                addedColonyCount++;

                var processedColonyCount = colonyIndex + 1;
                if (processedColonyCount == 1 || processedColonyCount % 1_000 == 0 || processedColonyCount == colonies.Count)
                {
                    var colonyPercent = CalculatePhasePercent(72, 77, processedColonyCount, colonies.Count);
                    await ReportImportProgressAsync(progress, colonyPercent, "Writing colonies...", $"Prepared {processedColonyCount:N0} of {colonies.Count:N0} colony record(s).");
                }
            }

            await ReportImportProgressAsync(progress, 78, "Writing history events...", $"Preparing {historyEvents.Count:N0} history event record(s).");
            for (var historyIndex = 0; historyIndex < historyEvents.Count; historyIndex++)
            {
                var historyEvent = historyEvents[historyIndex];
                var historyKey = BuildHistoryKey(historyEvent);
                if (existingHistoryByKey.TryGetValue(historyKey, out var existingHistory))
                {
                    skippedHistoryCount++;
                    if (MergeMissingHistoryData(existingHistory, historyEvent))
                    {
                        mergedHistoryCount++;
                    }

                    continue;
                }

                existingHistoryKeys.Add(historyKey);
                existingHistoryByKey[historyKey] = historyEvent;
                importedHistoryEvents.Add(historyEvent);
                addedHistoryCount++;

                var processedHistoryCount = historyIndex + 1;
                if (processedHistoryCount == 1 || processedHistoryCount % 5_000 == 0 || processedHistoryCount == historyEvents.Count)
                {
                    var historyPercent = CalculatePhasePercent(78, 82, processedHistoryCount, historyEvents.Count);
                    await ReportImportProgressAsync(progress, historyPercent, "Writing history events...", $"Prepared {processedHistoryCount:N0} of {historyEvents.Count:N0} history event record(s).");
                }
            }

            await BulkInsertCivilizationBatchAsync(
                dbContext,
                progress,
                83,
                $"Committing {addedColonyCount:N0} colonie(s), {importedColonies.Sum(colony => colony.Demographics.Count):N0} demographic row(s), and {addedHistoryCount:N0} history event(s).",
                importedColonies,
                importedHistoryEvents,
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

            await ReportImportProgressAsync(progress, 95, "Updating empire state...", "Setting fallen-empire flags from the finalized colony control data.");
            await ApplyStoredFallenEmpireFlagsAsync(dbContext, knownEmpires.Keys, cancellationToken);

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
                dbContext,
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
                $"Backfilled {mergedSystemCount:N0} existing star systems, {mergedWorldCount:N0} worlds, {mergedAlienCount:N0} races, {mergedEmpireCount:N0} empires, {mergedColonyCount:N0} colonies, and {mergedHistoryCount:N0} history events with missing data only.",
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
        StarWinDbContext dbContext,
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

    private async Task BulkInsertStarMapBatchAsync(
        StarWinDbContext dbContext,
        IProgress<StarWinLegacyImportProgress>? progress,
        int percentComplete,
        string detail,
        StarMapImportBatch batch,
        CancellationToken cancellationToken)
    {
        await ReportImportProgressAsync(
            progress,
            Math.Clamp(percentComplete - 1, 0, 100),
            "Saving star systems and worlds...",
            $"{detail} Using bulk database inserts.");

        if (batch.StarSystems.Count > 0)
        {
            await BulkInsertAsync(dbContext, "StarSystems", StarSystemColumns, batch.StarSystems.Select(BuildStarSystemValues), cancellationToken);
        }

        if (batch.AstralBodies.Count > 0)
        {
            await BulkInsertAsync(dbContext, "AstralBodies", AstralBodyColumns, batch.AstralBodies.Select(BuildAstralBodyValues), cancellationToken);
        }

        if (batch.Worlds.Count > 0)
        {
            await BulkInsertAsync(dbContext, "Worlds", WorldColumns, batch.Worlds.Select(BuildWorldValues), cancellationToken);
        }

        if (batch.UnusualCharacteristics.Count > 0)
        {
            await BulkInsertAsync(
                dbContext,
                "UnusualCharacteristics",
                UnusualCharacteristicColumns,
                batch.UnusualCharacteristics.Select(BuildUnusualCharacteristicValues),
                cancellationToken);
        }

        await ReportImportProgressAsync(
            progress,
            percentComplete,
            "Saving star systems and worlds...",
            $"{detail} Bulk database batch committed.");
    }

    private async Task BulkInsertCivilizationBatchAsync(
        StarWinDbContext dbContext,
        IProgress<StarWinLegacyImportProgress>? progress,
        int percentComplete,
        string detail,
        IReadOnlyList<Colony> colonies,
        IReadOnlyList<HistoryEvent> historyEvents,
        CancellationToken cancellationToken)
    {
        await ReportImportProgressAsync(
            progress,
            Math.Clamp(percentComplete - 1, 0, 100),
            "Saving colonies and history...",
            $"{detail} Using bulk database inserts.");

        if (colonies.Count > 0)
        {
            await ReportImportProgressAsync(progress, 82, "Saving colonies and history...", $"Bulk inserting {colonies.Count:N0} colony row(s).");
            await BulkInsertAsync(dbContext, "Colonies", ColonyColumns, colonies.Select(BuildColonyValues), cancellationToken);
        }

        var demographics = colonies
            .SelectMany(colony => colony.Demographics)
            .ToList();
        if (demographics.Count > 0)
        {
            await ReportImportProgressAsync(progress, 82, "Saving colonies and history...", $"Bulk inserting {demographics.Count:N0} colony demographic row(s).");
            await BulkInsertAsync(dbContext, "ColonyDemographics", ColonyDemographicColumns, demographics.Select(BuildColonyDemographicValues), cancellationToken);
        }

        if (historyEvents.Count > 0)
        {
            await ReportImportProgressAsync(progress, 82, "Saving colonies and history...", $"Bulk inserting {historyEvents.Count:N0} history event row(s).");
            await BulkInsertAsync(dbContext, "HistoryEvents", HistoryEventColumns, historyEvents.Select(BuildHistoryEventValues), cancellationToken);
        }

        await ReportImportProgressAsync(progress, percentComplete, "Saving colonies and history...", "Bulk database batch committed.");
    }

    private async Task BulkInsertCivilizationReferenceBatchAsync(
        StarWinDbContext dbContext,
        IProgress<StarWinLegacyImportProgress>? progress,
        int percentComplete,
        string detail,
        IReadOnlyList<AlienRace> alienRaces,
        IReadOnlyList<Empire> empires,
        CancellationToken cancellationToken)
    {
        await ReportImportProgressAsync(
            progress,
            Math.Clamp(percentComplete - 1, 0, 100),
            "Saving races, empires, and contacts...",
            $"{detail} Using bulk database inserts.");

        if (alienRaces.Count > 0)
        {
            await ReportImportProgressAsync(progress, 68, "Saving races, empires, and contacts...", $"Bulk inserting {alienRaces.Count:N0} alien race row(s).");
            await BulkInsertAsync(dbContext, "AlienRaces", AlienRaceColumns, alienRaces.Select(BuildAlienRaceValues), cancellationToken);
        }

        if (empires.Count > 0)
        {
            await ReportImportProgressAsync(progress, 69, "Saving races, empires, and contacts...", $"Bulk inserting {empires.Count:N0} empire row(s).");
            await BulkInsertAsync(dbContext, "Empires", EmpireColumns, empires.Select(BuildEmpireValues), cancellationToken);
        }

        var raceMemberships = empires
            .SelectMany(empire => empire.RaceMemberships)
            .ToList();
        if (raceMemberships.Count > 0)
        {
            await ReportImportProgressAsync(progress, 69, "Saving races, empires, and contacts...", $"Bulk inserting {raceMemberships.Count:N0} empire race membership row(s).");
            await BulkInsertAsync(dbContext, "EmpireRaceMemberships", EmpireRaceMembershipColumns, raceMemberships.Select(BuildEmpireRaceMembershipValues), cancellationToken);
        }

        var empireContacts = empires
            .SelectMany(empire => empire.Contacts)
            .ToList();
        if (empireContacts.Count > 0)
        {
            await ReportImportProgressAsync(progress, 69, "Saving races, empires, and contacts...", $"Bulk inserting {empireContacts.Count:N0} empire contact row(s).");
            await BulkInsertAsync(dbContext, "EmpireContacts", EmpireContactColumns, empireContacts.Select(BuildEmpireContactValues), cancellationToken);
        }

        await ReportImportProgressAsync(progress, percentComplete, "Saving races, empires, and contacts...", "Bulk database batch committed.");
    }

    private async Task BulkInsertAsync(
        StarWinDbContext dbContext,
        string tableName,
        IReadOnlyList<string> columns,
        IEnumerable<object?[]> rows,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            await BulkInsertSqlServerAsync(dbContext, tableName, columns, rows, cancellationToken);
            return;
        }

        await BulkInsertPreparedStatementsAsync(dbContext, tableName, columns, rows, cancellationToken);
    }

    private async Task BulkInsertSqlServerAsync(
        StarWinDbContext dbContext,
        string tableName,
        IReadOnlyList<string> columns,
        IEnumerable<object?[]> rows,
        CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var transaction = (SqlTransaction?)dbContext.Database.CurrentTransaction?.GetDbTransaction();
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints, transaction)
        {
            DestinationTableName = tableName,
            BatchSize = SqlBulkCopyBatchSize,
            BulkCopyTimeout = 0,
            EnableStreaming = true
        };

        foreach (var column in columns)
        {
            bulkCopy.ColumnMappings.Add(column, column);
        }

        using var table = CreateDataTable(columns, rows);
        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }

    private async Task BulkInsertPreparedStatementsAsync(
        StarWinDbContext dbContext,
        string tableName,
        IReadOnlyList<string> columns,
        IEnumerable<object?[]> rows,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = $"INSERT INTO {QuoteIdentifier(tableName)} ({string.Join(", ", columns.Select(QuoteIdentifier))}) VALUES ({string.Join(", ", columns.Select((_, index) => $"@p{index}"))});";

        for (var index = 0; index < columns.Count; index++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@p{index}";
            command.Parameters.Add(parameter);
        }

        foreach (var row in rows)
        {
            for (var index = 0; index < columns.Count; index++)
            {
                ((DbParameter)command.Parameters[index]).Value = row[index] ?? DBNull.Value;
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static DataTable CreateDataTable(IReadOnlyList<string> columns, IEnumerable<object?[]> rows)
    {
        var materializedRows = rows
            .Select(row => row.Select(value => value ?? DBNull.Value).ToArray())
            .ToList();
        var table = new DataTable();
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            var dataType = materializedRows
                .Select(row => row[columnIndex])
                .FirstOrDefault(value => value is not DBNull)
                ?.GetType()
                ?? typeof(object);
            table.Columns.Add(columns[columnIndex], dataType);
        }

        foreach (var row in materializedRows)
        {
            table.Rows.Add(row);
        }

        return table;
    }

    private static void AddAstralBodyRows(ICollection<AstralBodyImportRow> rows, int starSystemId, IEnumerable<AstralBody> astralBodies)
    {
        foreach (var astralBody in astralBodies)
        {
            rows.Add(new AstralBodyImportRow(starSystemId, astralBody));
        }
    }

    private static void AddUnusualCharacteristicRows(ICollection<UnusualCharacteristicImportRow> rows, World world)
    {
        foreach (var characteristic in world.UnusualCharacteristics)
        {
            rows.Add(new UnusualCharacteristicImportRow(world.Id, characteristic));
        }
    }

    private static object?[] BuildStarSystemValues(StarSystem system) =>
    [
        system.Id,
        system.SectorId,
        system.LegacySystemId,
        system.Name,
        FormatCoordinates(system.Coordinates),
        (int)system.AllegianceId,
        system.MapCode
    ];

    private static object?[] BuildAstralBodyValues(AstralBodyImportRow row) =>
    [
        row.Body.Role.ToString(),
        row.Body.Kind.ToString(),
        row.Body.Classification,
        row.Body.ClassificationCode,
        row.Body.DecimalClassCode,
        row.Body.PlanetCount,
        row.Body.Luminosity,
        row.Body.SolarMasses,
        row.Body.CompanionOrbitAu,
        row.StarSystemId
    ];

    private static object?[] BuildWorldValues(World world) =>
    [
        world.Id,
        world.Kind.ToString(),
        world.LegacyPlanetId,
        world.LegacyMoonId,
        world.StarSystemId,
        world.ParentWorldId,
        world.PrimaryAstralBodySequence,
        world.Name,
        world.WorldType,
        world.AtmosphereType,
        world.AtmosphereComposition,
        world.WaterType,
        world.Hydrography.WaterPercent,
        world.Hydrography.IcePercent,
        world.Hydrography.CloudPercent,
        world.MineralResources.MetalOre,
        world.MineralResources.RadioactiveOre,
        world.MineralResources.PreciousMetal,
        world.MineralResources.RawCrystals,
        world.MineralResources.PreciousGems,
        world.DiameterKm,
        world.DensityTenthsEarth,
        world.AtmosphericPressure,
        world.AverageTemperatureCelsius,
        world.MiscellaneousFlags,
        world.Albedo,
        world.AlienRaceId,
        world.ControlledByEmpireId,
        (int)world.AllegianceId,
        world.OrbitRadiusAu,
        world.OrbitRadiusKm,
        world.SmallestMolecularWeightRetained,
        world.AxialTiltDegrees,
        world.OrbitalInclinationDegrees,
        world.RotationPeriodHours is null ? null : (long)world.RotationPeriodHours.Value,
        world.EccentricityThousandths,
        world.OrbitPeriodDays,
        world.GravityEarthG,
        world.MassEarthMasses,
        world.EscapeVelocityKmPerSecond,
        world.OxygenPressureAtmospheres,
        world.BoilingPointCelsius,
        world.MagneticFieldGauss
    ];

    private static object?[] BuildUnusualCharacteristicValues(UnusualCharacteristicImportRow row) =>
    [
        row.Characteristic.Code,
        row.Characteristic.Name,
        row.Characteristic.Notes,
        row.WorldId
    ];

    private static object?[] BuildColonyValues(Colony colony) =>
    [
        colony.Id,
        colony.WorldId,
        colony.Name,
        colony.WorldKind.ToString(),
        (int)colony.RaceId,
        colony.ColonistRaceName,
        (int)colony.AllegianceId,
        colony.AllegianceName,
        colony.PoliticalStatus.ToString(),
        colony.ControllingEmpireId,
        colony.ParentEmpireId,
        colony.FoundingEmpireId,
        colony.EncodedPopulation,
        colony.EstimatedPopulation,
        colony.NativePopulationPercent,
        colony.ColonyClass,
        colony.ColonyClassCode,
        colony.Crime,
        colony.Law,
        colony.Stability,
        colony.AgeCenturies,
        colony.Starport,
        colony.StarportCode,
        colony.GovernmentType,
        colony.GovernmentTypeCode,
        colony.GrossWorldProductMcr,
        colony.MilitaryPower,
        colony.ExportResource,
        colony.ExportResourceCode,
        colony.ImportResource,
        colony.ImportResourceCode
    ];

    private static object?[] BuildColonyDemographicValues(ColonyDemographic demographic) =>
    [
        demographic.ColonyId,
        demographic.RaceId,
        demographic.RaceName,
        demographic.PopulationPercent,
        demographic.Population
    ];

    private static object?[] BuildHistoryEventValues(HistoryEvent historyEvent) =>
    [
        historyEvent.SectorId,
        historyEvent.Century,
        historyEvent.EventType,
        historyEvent.RaceId,
        historyEvent.OtherRaceId,
        historyEvent.EmpireId,
        historyEvent.ColonyId,
        historyEvent.PlanetId,
        historyEvent.StarSystemId,
        historyEvent.Description,
        historyEvent.ImportDataJson
    ];

    private static object?[] BuildAlienRaceValues(AlienRace alienRace) =>
    [
        alienRace.Id,
        alienRace.HomePlanetId,
        alienRace.Name,
        alienRace.EnvironmentType,
        alienRace.BodyChemistry,
        alienRace.BodyCoverType,
        alienRace.AppearanceType,
        alienRace.Diet,
        alienRace.Reproduction,
        alienRace.ReproductionMethod,
        alienRace.Devotion,
        alienRace.DevotionLevel.ToString(),
        alienRace.CivilizationProfile.Militancy,
        alienRace.CivilizationProfile.Determination,
        alienRace.CivilizationProfile.RacialTolerance,
        alienRace.CivilizationProfile.Progressiveness,
        alienRace.CivilizationProfile.Loyalty,
        alienRace.CivilizationProfile.SocialCohesion,
        alienRace.CivilizationProfile.Art,
        alienRace.CivilizationProfile.Individualism,
        alienRace.BiologyProfile.PsiPower,
        alienRace.BiologyProfile.PsiRating.ToString(),
        alienRace.BiologyProfile.Body,
        alienRace.BiologyProfile.Mind,
        alienRace.BiologyProfile.Speed,
        alienRace.BiologyProfile.Lifespan,
        alienRace.GravityPreference,
        alienRace.TemperaturePreference,
        alienRace.AtmosphereBreathed,
        alienRace.MassKg,
        alienRace.SizeCm,
        alienRace.LimbPairCount,
        SerializeImportData(alienRace.LimbTypes),
        SerializeImportData(alienRace.Abilities),
        SerializeImportData(alienRace.BodyCharacteristics),
        SerializeImportData(alienRace.EyeCharacteristics),
        SerializeImportData(alienRace.EyeColors),
        SerializeImportData(alienRace.HairColors),
        alienRace.HairType,
        SerializeImportData(alienRace.Colors),
        alienRace.ColorPattern,
        alienRace.LegacyAttributes,
        alienRace.RequiresUserRename,
        alienRace.ImportDataJson
    ];

    private static object?[] BuildEmpireValues(Empire empire) =>
    [
        empire.Id,
        empire.Name,
        empire.LegacyRaceId,
        empire.GovernmentType,
        empire.IsFallen,
        empire.CivilizationProfile.Militancy,
        empire.CivilizationProfile.Determination,
        empire.CivilizationProfile.RacialTolerance,
        empire.CivilizationProfile.Progressiveness,
        empire.CivilizationProfile.Loyalty,
        empire.CivilizationProfile.SocialCohesion,
        empire.CivilizationProfile.TechLevel,
        empire.CivilizationProfile.Art,
        empire.CivilizationProfile.Individualism,
        empire.CivilizationProfile.SpatialAge,
        empire.CivilizationModifiers.Militancy,
        empire.CivilizationModifiers.Determination,
        empire.CivilizationModifiers.RacialTolerance,
        empire.CivilizationModifiers.Progressiveness,
        empire.CivilizationModifiers.Loyalty,
        empire.CivilizationModifiers.SocialCohesion,
        empire.CivilizationModifiers.Art,
        empire.CivilizationModifiers.Individualism,
        empire.Founding.Origin.ToString(),
        empire.Founding.FoundingWorldId,
        empire.Founding.FoundingColonyId,
        empire.Founding.ParentEmpireId,
        empire.Founding.FoundingRaceId,
        empire.Founding.FoundedCentury,
        empire.ExpansionPolicy.ToString(),
        empire.EconomicPowerMcr,
        empire.MilitaryPower,
        empire.TradeBonusMcr,
        empire.Planets,
        empire.CaptivePlanets,
        empire.Moons,
        empire.SubjugatedPlanets,
        empire.SubjugatedMoons,
        empire.IndependentColonies,
        empire.SpaceHabitats,
        empire.NativePopulationMillions,
        empire.CaptivePopulationMillions,
        empire.SubjectPopulationMillions,
        empire.IndependentPopulationMillions,
        empire.MilitaryForces.Personnel.CrewRating,
        empire.MilitaryForces.Personnel.CrewQuality,
        empire.MilitaryForces.Personnel.TroopRating,
        empire.MilitaryForces.Personnel.TroopQuality,
        empire.MilitaryForces.Personnel.ConscriptionPolicy.ToString(),
        empire.MilitaryForces.NavyDoctrine.FighterEmphasisPercent,
        empire.MilitaryForces.NavyDoctrine.MissileEmphasisPercent,
        empire.MilitaryForces.NavyDoctrine.BeamWeaponEmphasisPercent,
        empire.MilitaryForces.NavyDoctrine.AssaultEmphasisPercent,
        empire.MilitaryForces.NavyDoctrine.DefenseEmphasisPercent,
        empire.MilitaryForces.Notes,
        empire.ImportDataJson
    ];

    private static object?[] BuildEmpireRaceMembershipValues(EmpireRaceMembership membership) =>
    [
        membership.EmpireId,
        membership.RaceId,
        membership.Role.ToString(),
        membership.PopulationMillions,
        membership.IsPrimary
    ];

    private static object?[] BuildEmpireContactValues(EmpireContact contact) =>
    [
        contact.EmpireId,
        contact.OtherEmpireId,
        contact.Relation,
        contact.RelationCode,
        contact.Age
    ];

    private static string SerializeImportData<T>(T importData) => JsonSerializer.Serialize(importData);

    private static string FormatCoordinates(Coordinates coordinates) => $"{coordinates.X},{coordinates.Y},{coordinates.Z}";

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

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
                    Description = Truncate(line, 1_200),
                    ImportDataJson = SerializeImportData(new
                    {
                        Line = line
                    })
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
                Description = Truncate(fields[6].Trim(), 1_200),
                ImportDataJson = SerializeImportData(new
                {
                    Century = fields[0].Trim(),
                    Type = fields[1].Trim(),
                    Race1 = fields[2].Trim(),
                    Race2 = fields[3].Trim(),
                    Planet = fields[4].Trim(),
                    System = fields[5].Trim(),
                    Event = fields[6].Trim()
                })
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

    private static AlienRace CreateAlienRace(
        LegacyAlienImport record,
        int sectorId,
        IReadOnlyDictionary<int, World> worldsById,
        ISet<string> usedRaceNames)
    {
        var homeWorldId = BuildPlanetWorldId(sectorId, Math.Abs(record.HomePlanetId));
        worldsById.TryGetValue(homeWorldId, out var homeWorld);
        var generatedName = GenerateSpeciesName(record, homeWorld, usedRaceNames);
        var atmosphereBreathed = homeWorld?.AtmosphereComposition ?? string.Empty;
        var gravity = homeWorld?.GravityEarthG ?? 0d;
        var gravityPreference = homeWorld is null
            ? string.Empty
            : $"{Math.Max(0.1d, gravity - 0.25d):0.0}-{gravity + 0.25d:0.0} g";
        var minTemperature = homeWorld?.AverageTemperatureCelsius - 10 ?? 0;
        var maxTemperature = homeWorld?.AverageTemperatureCelsius + 10 ?? 0;
        if (HasFlag(record.AbilityFlags, 1, 4))
        {
            maxTemperature += 5;
        }

        if (HasFlag(record.AbilityFlags, 1, 1))
        {
            maxTemperature -= 5;
        }

        return new AlienRace
        {
            Id = record.LegacyId,
            HomePlanetId = homeWorldId,
            Name = generatedName.Name,
            EnvironmentType = Lookup(EnvironmentTypes, record.EnvironmentType, "Environment"),
            BodyChemistry = Lookup(BodyChemistries, record.BodyType, "Body"),
            BodyCoverType = Lookup(BodyCoverTypes, record.BodyCoverType, "Body cover"),
            AppearanceType = Lookup(AppearanceTypes, record.AppearanceType, "Appearance"),
            Diet = Lookup(DietTypes, record.DietType, "Diet"),
            Reproduction = Lookup(ReproductionTypes, record.ReproductionType, "Reproduction"),
            ReproductionMethod = Lookup(ReproductionMethodTypes, record.ReproductionMethodType, "Reproduction method"),
            Devotion = record.Devotion,
            DevotionLevel = record.Devotion switch
            {
                0 => AlienDevotionLevel.None,
                <= 3 => AlienDevotionLevel.Poor,
                <= 6 => AlienDevotionLevel.Fair,
                <= 8 => AlienDevotionLevel.Good,
                _ => AlienDevotionLevel.High
            },
            CivilizationProfile = new CivilizationProfile
            {
                Militancy = GetAttribute(record.Attributes, 1),
                Determination = GetAttribute(record.Attributes, 2),
                RacialTolerance = GetAttribute(record.Attributes, 3),
                Progressiveness = GetAttribute(record.Attributes, 4),
                Loyalty = GetAttribute(record.Attributes, 5),
                SocialCohesion = GetAttribute(record.Attributes, 6),
                Art = GetAttribute(record.Attributes, 13),
                Individualism = GetAttribute(record.Attributes, 14)
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
            GravityPreference = gravityPreference,
            TemperaturePreference = homeWorld is null ? string.Empty : $"{minTemperature} to {maxTemperature} C",
            AtmosphereBreathed = atmosphereBreathed,
            MassKg = record.MassKg,
            SizeCm = record.SizeCm,
            LimbPairCount = record.LimbPairCount,
            LimbTypes = DecodeLimbTypes(record.LimbTypes, record.LimbPairCount),
            Abilities = DecodeFlags(record.AbilityFlags, SpecialAbilityTypes),
            BodyCharacteristics = DecodeBodyCharacteristics(record.BodyCharacteristicFlags),
            EyeCharacteristics = DecodeFlags(record.EyeCharacteristicFlags, EyeDetailTypes),
            EyeColors = DecodeFlags(record.EyeColorFlags, EyeColorTypes),
            HairColors = DecodeFlags(record.HairColorFlags, HairColorTypes),
            HairType = LookupZeroBased(HairTypes, record.HairType, "Hair type"),
            Colors = DecodeFlags(record.ColorFlags, ColorTypes),
            ColorPattern = GetColorPattern(record.BodyCharacteristicFlags),
            LegacyAttributes = record.Attributes,
            RequiresUserRename = generatedName.RequiresUserRename,
            ImportDataJson = SerializeImportData(BuildLegacyAlienImportData(record))
        };
    }

    private static Empire CreateEmpire(
        LegacyEmpireImport record,
        IReadOnlyDictionary<int, LegacyAlienImport> legacyAliensById,
        IReadOnlyDictionary<int, AlienRace> alienRacesById,
        IReadOnlyList<LegacyContactImport> contacts,
        int sectorId,
        ISet<string> usedEmpireNames)
    {
        legacyAliensById.TryGetValue(record.LegacyId, out var legacyAlien);
        alienRacesById.TryGetValue(record.LegacyId, out var alien);
        var governmentType = Lookup(GovernmentTypes, legacyAlien?.GovernmentType ?? 0, "Government");
        var religionType = Lookup(ReligionTypes, legacyAlien?.ReligionType ?? 0, "Religion");
        var empireName = GenerateEmpireName(alien?.Name ?? $"Empire {record.LegacyId}", governmentType, null, usedEmpireNames);
        var empire = new Empire
        {
            Id = record.LegacyId,
            Name = empireName,
            LegacyRaceId = record.LegacyId,
            GovernmentType = governmentType,
            CivilizationProfile = new CivilizationProfile
            {
                Militancy = alien?.CivilizationProfile.Militancy ?? 0,
                Determination = alien?.CivilizationProfile.Determination ?? 0,
                RacialTolerance = alien?.CivilizationProfile.RacialTolerance ?? 0,
                Progressiveness = alien?.CivilizationProfile.Progressiveness ?? 0,
                Loyalty = alien?.CivilizationProfile.Loyalty ?? 0,
                SocialCohesion = alien?.CivilizationProfile.SocialCohesion ?? 0,
                TechLevel = GetAttribute(alien?.LegacyAttributes, 8),
                Art = alien?.CivilizationProfile.Art ?? 0,
                Individualism = alien?.CivilizationProfile.Individualism ?? 0,
                SpatialAge = GetAttribute(alien?.LegacyAttributes, 15)
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
            Founding = new EmpireFounding
            {
                Origin = EmpireOrigin.NativeHomeworld,
                FoundingRaceId = record.LegacyId,
                FoundingWorldId = alien?.HomePlanetId
            },
            ImportDataJson = SerializeImportData(BuildLegacyEmpireImportData(record, legacyAlien, alien, empireName, governmentType, religionType))
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

        empire.Religions.Add(new EmpireReligion
        {
            EmpireId = empire.Id,
            ReligionId = 0,
            ReligionName = GenerateReligionName(alien?.Name ?? $"Empire {record.LegacyId}", religionType),
            PopulationPercent = 100m
        });

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

    private async Task<int> GetNextSectorIdAsync(StarWinDbContext dbContext, CancellationToken cancellationToken)
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

    private static async Task ApplyStoredFallenEmpireFlagsAsync(
        StarWinDbContext dbContext,
        IReadOnlyCollection<int> empireIds,
        CancellationToken cancellationToken)
    {
        if (empireIds.Count == 0)
        {
            return;
        }

        var colonies = await dbContext.Colonies.ToListAsync(cancellationToken);
        var controlledEmpireIds = colonies
            .Where(colony => colony.ControllingEmpireId.HasValue)
            .Select(colony => colony.ControllingEmpireId!.Value)
            .ToHashSet();
        var colonyLinkedEmpireIds = colonies
            .Where(colony => colony.ControllingEmpireId.HasValue)
            .Select(colony => colony.ControllingEmpireId!.Value)
            .Concat(colonies
                .Where(colony => colony.FoundingEmpireId.HasValue)
                .Select(colony => colony.FoundingEmpireId!.Value))
            .ToHashSet();

        var persistedEmpires = await dbContext.Empires
            .Where(empire => empireIds.Contains(empire.Id))
            .ToListAsync(cancellationToken);
        var empiresToUpdate = persistedEmpires
            .Concat(dbContext.ChangeTracker
                .Entries<Empire>()
                .Where(entry => entry.State == EntityState.Added && empireIds.Contains(entry.Entity.Id))
                .Select(entry => entry.Entity))
            .DistinctBy(empire => empire.Id)
            .ToList();

        foreach (var empire in empiresToUpdate)
        {
            empire.IsFallen = DetermineStoredFallenEmpireStatus(empire, controlledEmpireIds, colonyLinkedEmpireIds);
        }
    }

    private static bool DetermineStoredFallenEmpireStatus(
        Empire empire,
        IReadOnlySet<int> controlledEmpireIds,
        IReadOnlySet<int> colonyLinkedEmpireIds)
    {
        return !controlledEmpireIds.Contains(empire.Id)
            && (colonyLinkedEmpireIds.Contains(empire.Id)
                || empire.Planets > 0
                || empire.Moons > 0
                || empire.SpaceHabitats > 0
                || empire.NativePopulationMillions > 0
                || empire.SubjectPopulationMillions > 0);
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

    private static GeneratedNameResult GenerateSpeciesName(LegacyAlienImport record, World? homeWorld, ISet<string> usedRaceNames)
    {
        if (!IsPlaceholderName(record.Name))
        {
            usedRaceNames.Add(record.Name.Trim());
            return new GeneratedNameResult(record.Name.Trim(), false);
        }

        var baseRoot = homeWorld is not null
            ? NormalizeNameRoot(homeWorld.Name)
            : string.Empty;
        if (string.IsNullOrWhiteSpace(baseRoot))
        {
            return new GeneratedNameResult($"Race {record.LegacyId}", true);
        }

        var baseName = BuildSpeciesNameFromRoot(baseRoot);
        if (usedRaceNames.Add(baseName))
        {
            return new GeneratedNameResult(baseName, false);
        }

        foreach (var variant in GenerateSpeciesVariants(baseName, baseRoot))
        {
            if (usedRaceNames.Add(variant))
            {
                return new GeneratedNameResult(variant, false);
            }
        }

        return new GeneratedNameResult(baseName, true);
    }

    private static string GenerateEmpireName(string speciesName, string governmentType, string? colonyWorldName, ISet<string> usedEmpireNames)
    {
        foreach (var template in GetGovernmentTemplates(governmentType))
        {
            var candidate = ApplyNameTemplate(template, speciesName);
            if (usedEmpireNames.Add(candidate))
            {
                return candidate;
            }
        }

        var colonyRoot = NormalizeNameRoot(colonyWorldName);
        if (!string.IsNullOrWhiteSpace(colonyRoot))
        {
            foreach (var template in GetGovernmentTemplates(governmentType))
            {
                var candidate = ApplyNameTemplate(template, colonyRoot);
                if (usedEmpireNames.Add(candidate))
                {
                    return candidate;
                }
            }
        }

        var suffix = 2;
        while (true)
        {
            var candidate = $"{speciesName} {governmentType} {suffix}";
            if (usedEmpireNames.Add(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }

    private static string GenerateReligionName(string speciesName, string religionType)
    {
        return religionType switch
        {
            "Animism" => $"{speciesName} Tradition",
            "Polytheism" => $"{speciesName} Pantheon",
            "Dualism" => $"{speciesName} Doctrine",
            "Monotheism" => $"{speciesName} Church",
            "Deism" => $"{speciesName} Path",
            "Pantheism" => $"{speciesName} Faith",
            "Agnosticism" => $"{speciesName} Philosophy",
            "Rational atheism" => $"{speciesName} Rationalists",
            "Philosophical atheism" => $"{speciesName} Humanists",
            "Leader worship" => $"{speciesName} Cult",
            "Multiple monotheism" => $"{speciesName} Communion",
            _ => $"{speciesName} Faith"
        };
    }

    private static string InferReligionTypeFromName(string religionName)
    {
        return religionName switch
        {
            var name when name.Contains("Pantheon", StringComparison.OrdinalIgnoreCase) => "Polytheism",
            var name when name.Contains("Church", StringComparison.OrdinalIgnoreCase) => "Monotheism",
            var name when name.Contains("Cult", StringComparison.OrdinalIgnoreCase) => "Leader worship",
            var name when name.Contains("Doctrine", StringComparison.OrdinalIgnoreCase) => "Dualism",
            var name when name.Contains("Path", StringComparison.OrdinalIgnoreCase) => "Deism",
            var name when name.Contains("Tradition", StringComparison.OrdinalIgnoreCase) => "Animism",
            _ => "Religion"
        };
    }

    private static bool IsPlaceholderName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var trimmed = name.Trim();
        return trimmed.StartsWith("Race ", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeNameRoot(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            var suffix = parts[^1];
            if (int.TryParse(suffix, out _)
                || IsRomanNumeral(suffix)
                || (suffix.Length == 1 && char.IsLetter(suffix[0])))
            {
                trimmed = string.Join(' ', parts[..^1]);
            }
        }

        return trimmed.Trim();
    }

    private static bool IsRomanNumeral(string value)
    {
        return value.All(character => "IVXLCDMivxlcdm".Contains(character));
    }

    private static string BuildSpeciesNameFromRoot(string root)
    {
        if (root.EndsWith("a", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root[..^1]}an";
        }

        if (root.EndsWith("earth", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root}ling";
        }

        if (root.EndsWith("sol", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root}an";
        }

        if (root.EndsWith("erra", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root[..^1]}n";
        }

        if (root.EndsWith("el", StringComparison.OrdinalIgnoreCase)
            || root.EndsWith("il", StringComparison.OrdinalIgnoreCase)
            || root.EndsWith("ol", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root}ian";
        }

        return $"{root}ian";
    }

    private static IEnumerable<string> GenerateSpeciesVariants(string baseName, string root)
    {
        foreach (var prefix in UniversalSpeciesPrefixes)
        {
            yield return $"{prefix}{baseName}";
        }

        foreach (var suffix in UniversalSpeciesSuffixes)
        {
            yield return $"{root}{suffix}";
        }

        foreach (var prefix in UniversalSpeciesPrefixes)
        {
            foreach (var suffix in UniversalSpeciesSuffixes)
            {
                yield return $"{prefix}{root}{suffix}";
            }
        }
    }

    private static IReadOnlyList<string> GetGovernmentTemplates(string governmentType)
    {
        return governmentType switch
        {
            "Imperialism" => ["{0} Empire", "{0} Imperium", "Imperium of {0}", "{0} Dominion"],
            "Democracy" or "Republic" or "Federation" => ["{0} Republic", "Republic of {0}", "{0} Federation", "{0} Union"],
            "Theocracy" => ["{0} Church", "{0} Covenant", "{0} Theocracy", "Holy State of {0}"],
            "Corporation" => ["{0} Combine", "{0} Consortium", "{0} Syndicate", "{0} Holdings"],
            "Monarchy" => ["Kingdom of {0}", "{0} Kingdom", "{0} Crown", "{0} Realm"],
            _ => ["{0} State", "{0} Compact", "{0} Assembly", "{0} Accord"]
        };
    }

    private static string ApplyNameTemplate(string template, string value)
    {
        return string.Format(CultureInfo.InvariantCulture, template, value).Trim();
    }

    private static List<string> DecodeLimbTypes(IReadOnlyList<byte> limbTypes, byte limbPairCount)
    {
        var results = new List<string>();
        for (var index = 0; index < limbPairCount && index < limbTypes.Count; index++)
        {
            var role = limbTypes[index] < LimbRoleTypes.Length
                ? LimbRoleTypes[limbTypes[index]]
                : $"Limb type {limbTypes[index]}";
            results.Add($"Pair {index + 1}: {role}");
        }

        if (results.Count == 0 && limbPairCount == 0)
        {
            results.Add("None");
        }

        return results;
    }

    private static List<string> DecodeBodyCharacteristics(IReadOnlyList<byte> flags)
    {
        return DecodeFlags(flags, BodyPartTypes)
            .Where(value => value is not "One color" and not "Two-tone" and not "Multi color" and not "Striped/Banded" and not "Spots" and not "Randomly mottled")
            .ToList();
    }

    private static string GetColorPattern(IReadOnlyList<byte> flags)
    {
        return DecodeFlags(flags, BodyPartTypes)
            .FirstOrDefault(value => value is "One color" or "Two-tone" or "Multi color" or "Striped/Banded" or "Spots" or "Randomly mottled")
            ?? string.Empty;
    }

    private static List<string> DecodeFlags(IReadOnlyList<byte> flags, IReadOnlyList<string> labels)
    {
        var results = new List<string>();
        for (var byteIndex = 0; byteIndex < flags.Count; byteIndex++)
        {
            for (var bitIndex = 0; bitIndex < 8; bitIndex++)
            {
                if ((flags[byteIndex] & (1 << bitIndex)) == 0)
                {
                    continue;
                }

                var labelIndex = byteIndex * 8 + bitIndex;
                if (labelIndex >= labels.Count)
                {
                    continue;
                }

                results.Add(labels[labelIndex]);
            }
        }

        return results;
    }

    private static object BuildLegacyAlienImportData(LegacyAlienImport record)
    {
        return new
        {
            record.LegacyId,
            record.HomePlanetId,
            Name = record.Name,
            EnvironmentType = Lookup(EnvironmentTypes, record.EnvironmentType, "Environment"),
            BodyType = Lookup(BodyChemistries, record.BodyType, "Body"),
            LimbPairCount = record.LimbPairCount,
            LimbTypes = DecodeLimbTypes(record.LimbTypes, record.LimbPairCount),
            DietType = Lookup(DietTypes, record.DietType, "Diet"),
            ReproductionType = Lookup(ReproductionTypes, record.ReproductionType, "Reproduction"),
            ReproductionMethodType = Lookup(ReproductionMethodTypes, record.ReproductionMethodType, "Reproduction method"),
            GovernmentType = Lookup(GovernmentTypes, record.GovernmentType, "Government"),
            BodyCoverType = Lookup(BodyCoverTypes, record.BodyCoverType, "Body cover"),
            AppearanceType = Lookup(AppearanceTypes, record.AppearanceType, "Appearance"),
            record.MassKg,
            record.SizeCm,
            Attributes = BuildLegacyAlienAttributeData(record.Attributes),
            Abilities = DecodeFlags(record.AbilityFlags, SpecialAbilityTypes),
            BodyColors = DecodeFlags(record.ColorFlags, ColorTypes),
            HairColors = DecodeFlags(record.HairColorFlags, HairColorTypes),
            BodyCharacteristics = DecodeBodyCharacteristics(record.BodyCharacteristicFlags),
            ColorPattern = GetColorPattern(record.BodyCharacteristicFlags),
            EyeColors = DecodeFlags(record.EyeColorFlags, EyeColorTypes),
            EyeCharacteristics = DecodeFlags(record.EyeCharacteristicFlags, EyeDetailTypes),
            HairType = LookupZeroBased(HairTypes, record.HairType, "Hair type"),
            ReligionType = Lookup(ReligionTypes, record.ReligionType, "Religion"),
            record.Devotion
        };
    }

    private static object BuildLegacyEmpireImportData(
        LegacyEmpireImport record,
        LegacyAlienImport? legacyAlien,
        AlienRace? alien,
        string empireName,
        string governmentType,
        string religionType)
    {
        return new
        {
            record.LegacyId,
            GeneratedEmpireName = empireName,
            GovernmentType = governmentType,
            ReligionType = religionType,
            EmpireAttributes = new
            {
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
                IndependentColonies = GetEmpireAttributeAsInt(record.Attributes, 13)
            },
            SourceRace = legacyAlien is null
                ? null
                : new
                {
                    legacyAlien.LegacyId,
                    RaceName = alien?.Name ?? legacyAlien.Name,
                    TechLevel = GetAttribute(legacyAlien.Attributes, 8),
                    SpatialAge = GetAttribute(legacyAlien.Attributes, 15)
                }
        };
    }

    private static object BuildLegacyAlienAttributeData(IReadOnlyList<byte> attributes)
    {
        return new
        {
            Militancy = GetAttribute(attributes, 1),
            Determination = GetAttribute(attributes, 2),
            RacialTolerance = GetAttribute(attributes, 3),
            Progressiveness = GetAttribute(attributes, 4),
            Loyalty = GetAttribute(attributes, 5),
            SocialCohesion = GetAttribute(attributes, 6),
            PsiPower = GetAttribute(attributes, 7),
            TechLevel = GetAttribute(attributes, 8),
            Body = GetAttribute(attributes, 9),
            Mind = GetAttribute(attributes, 10),
            Speed = GetAttribute(attributes, 11),
            Lifespan = GetAttribute(attributes, 12),
            Art = GetAttribute(attributes, 13),
            Individualism = GetAttribute(attributes, 14),
            SpatialAge = GetAttribute(attributes, 15)
        };
    }

    private static bool HasFlag(IReadOnlyList<byte> flags, int oneBasedByteIndex, int oneBasedBitIndex)
    {
        if (oneBasedByteIndex < 1 || oneBasedByteIndex > flags.Count)
        {
            return false;
        }

        var value = flags[oneBasedByteIndex - 1];
        return (value & (1 << (oneBasedBitIndex - 1))) != 0;
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

    private static bool MergeMissingStarSystemData(StarSystem existing, StarSystem imported)
    {
        var changed = false;

        if (existing.LegacySystemId is null && imported.LegacySystemId is not null)
        {
            existing.LegacySystemId = imported.LegacySystemId;
            changed = true;
        }

        changed |= FillMissingString(() => existing.Name, value => existing.Name = value, imported.Name);
        if (existing.Coordinates == default && imported.Coordinates != default)
        {
            existing.Coordinates = imported.Coordinates;
            changed = true;
        }

        if (existing.AllegianceId == ushort.MaxValue && imported.AllegianceId != ushort.MaxValue)
        {
            existing.AllegianceId = imported.AllegianceId;
            changed = true;
        }

        if (existing.MapCode == 0 && imported.MapCode != 0)
        {
            existing.MapCode = imported.MapCode;
            changed = true;
        }

        return changed;
    }

    private static bool MergeMissingWorldData(World existing, World imported)
    {
        var changed = false;

        if (existing.LegacyPlanetId is null && imported.LegacyPlanetId is not null)
        {
            existing.LegacyPlanetId = imported.LegacyPlanetId;
            changed = true;
        }

        if (existing.LegacyMoonId is null && imported.LegacyMoonId is not null)
        {
            existing.LegacyMoonId = imported.LegacyMoonId;
            changed = true;
        }

        if (existing.StarSystemId is null && imported.StarSystemId is not null)
        {
            existing.StarSystemId = imported.StarSystemId;
            changed = true;
        }

        if (existing.ParentWorldId is null && imported.ParentWorldId is not null)
        {
            existing.ParentWorldId = imported.ParentWorldId;
            changed = true;
        }

        if (existing.PrimaryAstralBodySequence is null && imported.PrimaryAstralBodySequence is not null)
        {
            existing.PrimaryAstralBodySequence = imported.PrimaryAstralBodySequence;
            changed = true;
        }

        changed |= FillMissingString(() => existing.Name, value => existing.Name = value, imported.Name);
        changed |= FillMissingString(() => existing.WorldType, value => existing.WorldType = value, imported.WorldType);
        changed |= FillMissingString(() => existing.AtmosphereType, value => existing.AtmosphereType = value, imported.AtmosphereType);
        changed |= FillMissingString(() => existing.AtmosphereComposition, value => existing.AtmosphereComposition = value, imported.AtmosphereComposition);
        changed |= FillMissingString(() => existing.WaterType, value => existing.WaterType = value, imported.WaterType);

        if (existing.DiameterKm == 0 && imported.DiameterKm > 0)
        {
            existing.DiameterKm = imported.DiameterKm;
            changed = true;
        }

        if (existing.DensityTenthsEarth == 0 && imported.DensityTenthsEarth > 0)
        {
            existing.DensityTenthsEarth = imported.DensityTenthsEarth;
            changed = true;
        }

        if (existing.AlienRaceId is null && imported.AlienRaceId is not null)
        {
            existing.AlienRaceId = imported.AlienRaceId;
            changed = true;
        }

        if (existing.ControlledByEmpireId is null && imported.ControlledByEmpireId is not null)
        {
            existing.ControlledByEmpireId = imported.ControlledByEmpireId;
            changed = true;
        }

        if (existing.AllegianceId == ushort.MaxValue && imported.AllegianceId != ushort.MaxValue)
        {
            existing.AllegianceId = imported.AllegianceId;
            changed = true;
        }

        changed |= FillMissingNullable(() => existing.Albedo, value => existing.Albedo = value, imported.Albedo);
        changed |= FillMissingNullable(() => existing.OrbitRadiusAu, value => existing.OrbitRadiusAu = value, imported.OrbitRadiusAu);
        changed |= FillMissingNullable(() => existing.OrbitRadiusKm, value => existing.OrbitRadiusKm = value, imported.OrbitRadiusKm);
        changed |= FillMissingNullable(() => existing.SmallestMolecularWeightRetained, value => existing.SmallestMolecularWeightRetained = value, imported.SmallestMolecularWeightRetained);
        changed |= FillMissingNullable(() => existing.AxialTiltDegrees, value => existing.AxialTiltDegrees = value, imported.AxialTiltDegrees);
        changed |= FillMissingNullable(() => existing.OrbitalInclinationDegrees, value => existing.OrbitalInclinationDegrees = value, imported.OrbitalInclinationDegrees);
        changed |= FillMissingNullable(() => existing.RotationPeriodHours, value => existing.RotationPeriodHours = value, imported.RotationPeriodHours);
        changed |= FillMissingNullable(() => existing.EccentricityThousandths, value => existing.EccentricityThousandths = value, imported.EccentricityThousandths);
        changed |= FillMissingNullable(() => existing.OrbitPeriodDays, value => existing.OrbitPeriodDays = value, imported.OrbitPeriodDays);
        changed |= FillMissingNullable(() => existing.GravityEarthG, value => existing.GravityEarthG = value, imported.GravityEarthG);
        changed |= FillMissingNullable(() => existing.MassEarthMasses, value => existing.MassEarthMasses = value, imported.MassEarthMasses);
        changed |= FillMissingNullable(() => existing.EscapeVelocityKmPerSecond, value => existing.EscapeVelocityKmPerSecond = value, imported.EscapeVelocityKmPerSecond);
        changed |= FillMissingNullable(() => existing.OxygenPressureAtmospheres, value => existing.OxygenPressureAtmospheres = value, imported.OxygenPressureAtmospheres);
        changed |= FillMissingNullable(() => existing.BoilingPointCelsius, value => existing.BoilingPointCelsius = value, imported.BoilingPointCelsius);
        changed |= FillMissingNullable(() => existing.MagneticFieldGauss, value => existing.MagneticFieldGauss = value, imported.MagneticFieldGauss);

        if (existing.Hydrography.WaterPercent == 0
            && existing.Hydrography.IcePercent == 0
            && existing.Hydrography.CloudPercent == 0
            && (imported.Hydrography.WaterPercent > 0 || imported.Hydrography.IcePercent > 0 || imported.Hydrography.CloudPercent > 0))
        {
            existing.Hydrography.WaterPercent = imported.Hydrography.WaterPercent;
            existing.Hydrography.IcePercent = imported.Hydrography.IcePercent;
            existing.Hydrography.CloudPercent = imported.Hydrography.CloudPercent;
            changed = true;
        }

        if (existing.MineralResources.MetalOre == 0
            && existing.MineralResources.RadioactiveOre == 0
            && existing.MineralResources.PreciousMetal == 0
            && existing.MineralResources.RawCrystals == 0
            && existing.MineralResources.PreciousGems == 0
            && (imported.MineralResources.MetalOre > 0
                || imported.MineralResources.RadioactiveOre > 0
                || imported.MineralResources.PreciousMetal > 0
                || imported.MineralResources.RawCrystals > 0
                || imported.MineralResources.PreciousGems > 0))
        {
            existing.MineralResources.MetalOre = imported.MineralResources.MetalOre;
            existing.MineralResources.RadioactiveOre = imported.MineralResources.RadioactiveOre;
            existing.MineralResources.PreciousMetal = imported.MineralResources.PreciousMetal;
            existing.MineralResources.RawCrystals = imported.MineralResources.RawCrystals;
            existing.MineralResources.PreciousGems = imported.MineralResources.PreciousGems;
            changed = true;
        }

        var knownCharacteristicCodes = existing.UnusualCharacteristics
            .Select(characteristic => characteristic.Code)
            .ToHashSet();
        foreach (var characteristic in imported.UnusualCharacteristics.Where(characteristic => knownCharacteristicCodes.Add(characteristic.Code)))
        {
            existing.UnusualCharacteristics.Add(new UnusualCharacteristic
            {
                Code = characteristic.Code,
                Name = characteristic.Name,
                Notes = characteristic.Notes
            });
            changed = true;
        }

        return changed;
    }

    private static bool MergeMissingAlienRaceData(AlienRace existing, AlienRace imported)
    {
        var changed = false;

        if (existing.HomePlanetId == 0 && imported.HomePlanetId > 0)
        {
            existing.HomePlanetId = imported.HomePlanetId;
            changed = true;
        }

        changed |= FillMissingString(() => existing.Name, value => existing.Name = value, imported.Name);
        changed |= FillMissingString(() => existing.EnvironmentType, value => existing.EnvironmentType = value, imported.EnvironmentType);
        changed |= FillMissingString(() => existing.BodyChemistry, value => existing.BodyChemistry = value, imported.BodyChemistry);
        changed |= FillMissingString(() => existing.BodyCoverType, value => existing.BodyCoverType = value, imported.BodyCoverType);
        changed |= FillMissingString(() => existing.AppearanceType, value => existing.AppearanceType = value, imported.AppearanceType);
        changed |= FillMissingString(() => existing.Diet, value => existing.Diet = value, imported.Diet);
        changed |= FillMissingString(() => existing.Reproduction, value => existing.Reproduction = value, imported.Reproduction);
        changed |= FillMissingString(() => existing.ReproductionMethod, value => existing.ReproductionMethod = value, imported.ReproductionMethod);
        changed |= FillMissingString(() => existing.GravityPreference, value => existing.GravityPreference = value, imported.GravityPreference);
        changed |= FillMissingString(() => existing.TemperaturePreference, value => existing.TemperaturePreference = value, imported.TemperaturePreference);
        changed |= FillMissingString(() => existing.AtmosphereBreathed, value => existing.AtmosphereBreathed = value, imported.AtmosphereBreathed);
        changed |= FillMissingString(() => existing.ImportDataJson, value => existing.ImportDataJson = value, imported.ImportDataJson);

        if (existing.Devotion == 0 && imported.Devotion > 0)
        {
            existing.Devotion = imported.Devotion;
            changed = true;
        }

        if (existing.DevotionLevel == AlienDevotionLevel.None && imported.DevotionLevel != AlienDevotionLevel.None)
        {
            existing.DevotionLevel = imported.DevotionLevel;
            changed = true;
        }

        if (existing.MassKg == 0 && imported.MassKg > 0)
        {
            existing.MassKg = imported.MassKg;
            changed = true;
        }

        if (existing.SizeCm == 0 && imported.SizeCm > 0)
        {
            existing.SizeCm = imported.SizeCm;
            changed = true;
        }

        if (existing.LimbPairCount == 0 && imported.LimbPairCount > 0)
        {
            existing.LimbPairCount = imported.LimbPairCount;
            changed = true;
        }

        if (existing.BiologyProfile.PsiPower == 0 && imported.BiologyProfile.PsiPower > 0)
        {
            existing.BiologyProfile.PsiPower = imported.BiologyProfile.PsiPower;
            existing.BiologyProfile.PsiRating = imported.BiologyProfile.PsiRating;
            changed = true;
        }

        if (existing.BiologyProfile.Body == 0 && imported.BiologyProfile.Body > 0)
        {
            existing.BiologyProfile.Body = imported.BiologyProfile.Body;
            changed = true;
        }

        if (existing.BiologyProfile.Mind == 0 && imported.BiologyProfile.Mind > 0)
        {
            existing.BiologyProfile.Mind = imported.BiologyProfile.Mind;
            changed = true;
        }

        if (existing.BiologyProfile.Speed == 0 && imported.BiologyProfile.Speed > 0)
        {
            existing.BiologyProfile.Speed = imported.BiologyProfile.Speed;
            changed = true;
        }

        if (existing.BiologyProfile.Lifespan == 0 && imported.BiologyProfile.Lifespan > 0)
        {
            existing.BiologyProfile.Lifespan = imported.BiologyProfile.Lifespan;
            changed = true;
        }

        changed |= FillMissingByte(() => existing.CivilizationProfile.Militancy, value => existing.CivilizationProfile.Militancy = value, imported.CivilizationProfile.Militancy);
        changed |= FillMissingByte(() => existing.CivilizationProfile.Determination, value => existing.CivilizationProfile.Determination = value, imported.CivilizationProfile.Determination);
        changed |= FillMissingByte(() => existing.CivilizationProfile.RacialTolerance, value => existing.CivilizationProfile.RacialTolerance = value, imported.CivilizationProfile.RacialTolerance);
        changed |= FillMissingByte(() => existing.CivilizationProfile.Progressiveness, value => existing.CivilizationProfile.Progressiveness = value, imported.CivilizationProfile.Progressiveness);
        changed |= FillMissingByte(() => existing.CivilizationProfile.Loyalty, value => existing.CivilizationProfile.Loyalty = value, imported.CivilizationProfile.Loyalty);
        changed |= FillMissingByte(() => existing.CivilizationProfile.SocialCohesion, value => existing.CivilizationProfile.SocialCohesion = value, imported.CivilizationProfile.SocialCohesion);
        changed |= FillMissingByte(() => existing.CivilizationProfile.Art, value => existing.CivilizationProfile.Art = value, imported.CivilizationProfile.Art);
        changed |= FillMissingByte(() => existing.CivilizationProfile.Individualism, value => existing.CivilizationProfile.Individualism = value, imported.CivilizationProfile.Individualism);

        if ((existing.LegacyAttributes?.Length ?? 0) == 0 && (imported.LegacyAttributes?.Length ?? 0) > 0)
        {
            existing.LegacyAttributes = imported.LegacyAttributes ?? Array.Empty<byte>();
            changed = true;
        }

        changed |= AddMissingStrings(existing.LimbTypes, imported.LimbTypes);
        changed |= AddMissingStrings(existing.Abilities, imported.Abilities);
        changed |= AddMissingStrings(existing.BodyCharacteristics, imported.BodyCharacteristics);
        changed |= AddMissingStrings(existing.EyeCharacteristics, imported.EyeCharacteristics);
        changed |= AddMissingStrings(existing.EyeColors, imported.EyeColors);
        changed |= AddMissingStrings(existing.HairColors, imported.HairColors);
        changed |= AddMissingStrings(existing.Colors, imported.Colors);
        changed |= FillMissingString(() => existing.HairType, value => existing.HairType = value, imported.HairType);
        changed |= FillMissingString(() => existing.ColorPattern, value => existing.ColorPattern = value, imported.ColorPattern);

        return changed;
    }

    private static bool MergeMissingEmpireData(Empire existing, Empire imported)
    {
        var changed = false;

        changed |= FillMissingString(() => existing.Name, value => existing.Name = value, imported.Name);
        changed |= FillMissingNullable(() => existing.LegacyRaceId, value => existing.LegacyRaceId = value, imported.LegacyRaceId);
        changed |= FillMissingString(() => existing.GovernmentType, value => existing.GovernmentType = value, imported.GovernmentType);
        changed |= FillMissingString(() => existing.ImportDataJson, value => existing.ImportDataJson = value, imported.ImportDataJson);

        changed |= FillMissingByte(() => existing.CivilizationProfile.Militancy, value => existing.CivilizationProfile.Militancy = value, imported.CivilizationProfile.Militancy);
        changed |= FillMissingByte(() => existing.CivilizationProfile.Determination, value => existing.CivilizationProfile.Determination = value, imported.CivilizationProfile.Determination);
        changed |= FillMissingByte(() => existing.CivilizationProfile.RacialTolerance, value => existing.CivilizationProfile.RacialTolerance = value, imported.CivilizationProfile.RacialTolerance);
        changed |= FillMissingByte(() => existing.CivilizationProfile.Progressiveness, value => existing.CivilizationProfile.Progressiveness = value, imported.CivilizationProfile.Progressiveness);
        changed |= FillMissingByte(() => existing.CivilizationProfile.Loyalty, value => existing.CivilizationProfile.Loyalty = value, imported.CivilizationProfile.Loyalty);
        changed |= FillMissingByte(() => existing.CivilizationProfile.SocialCohesion, value => existing.CivilizationProfile.SocialCohesion = value, imported.CivilizationProfile.SocialCohesion);
        changed |= FillMissingByte(() => existing.CivilizationProfile.TechLevel, value => existing.CivilizationProfile.TechLevel = value, imported.CivilizationProfile.TechLevel);
        changed |= FillMissingByte(() => existing.CivilizationProfile.Art, value => existing.CivilizationProfile.Art = value, imported.CivilizationProfile.Art);
        changed |= FillMissingByte(() => existing.CivilizationProfile.Individualism, value => existing.CivilizationProfile.Individualism = value, imported.CivilizationProfile.Individualism);
        changed |= FillMissingByte(() => existing.CivilizationProfile.SpatialAge, value => existing.CivilizationProfile.SpatialAge = value, imported.CivilizationProfile.SpatialAge);
        changed |= FillMissingInt(() => existing.CivilizationModifiers.Militancy, value => existing.CivilizationModifiers.Militancy = value, imported.CivilizationModifiers.Militancy);
        changed |= FillMissingInt(() => existing.CivilizationModifiers.Determination, value => existing.CivilizationModifiers.Determination = value, imported.CivilizationModifiers.Determination);
        changed |= FillMissingInt(() => existing.CivilizationModifiers.RacialTolerance, value => existing.CivilizationModifiers.RacialTolerance = value, imported.CivilizationModifiers.RacialTolerance);
        changed |= FillMissingInt(() => existing.CivilizationModifiers.Progressiveness, value => existing.CivilizationModifiers.Progressiveness = value, imported.CivilizationModifiers.Progressiveness);
        changed |= FillMissingInt(() => existing.CivilizationModifiers.Loyalty, value => existing.CivilizationModifiers.Loyalty = value, imported.CivilizationModifiers.Loyalty);
        changed |= FillMissingInt(() => existing.CivilizationModifiers.SocialCohesion, value => existing.CivilizationModifiers.SocialCohesion = value, imported.CivilizationModifiers.SocialCohesion);
        changed |= FillMissingInt(() => existing.CivilizationModifiers.Art, value => existing.CivilizationModifiers.Art = value, imported.CivilizationModifiers.Art);
        changed |= FillMissingInt(() => existing.CivilizationModifiers.Individualism, value => existing.CivilizationModifiers.Individualism = value, imported.CivilizationModifiers.Individualism);

        changed |= FillMissingLong(() => existing.EconomicPowerMcr, value => existing.EconomicPowerMcr = value, imported.EconomicPowerMcr);
        changed |= FillMissingLong(() => existing.MilitaryPower, value => existing.MilitaryPower = value, imported.MilitaryPower);
        changed |= FillMissingLong(() => existing.TradeBonusMcr, value => existing.TradeBonusMcr = value, imported.TradeBonusMcr);
        changed |= FillMissingInt(() => existing.Planets, value => existing.Planets = value, imported.Planets);
        changed |= FillMissingInt(() => existing.CaptivePlanets, value => existing.CaptivePlanets = value, imported.CaptivePlanets);
        changed |= FillMissingInt(() => existing.Moons, value => existing.Moons = value, imported.Moons);
        changed |= FillMissingInt(() => existing.SubjugatedPlanets, value => existing.SubjugatedPlanets = value, imported.SubjugatedPlanets);
        changed |= FillMissingInt(() => existing.SubjugatedMoons, value => existing.SubjugatedMoons = value, imported.SubjugatedMoons);
        changed |= FillMissingInt(() => existing.IndependentColonies, value => existing.IndependentColonies = value, imported.IndependentColonies);
        changed |= FillMissingLong(() => existing.NativePopulationMillions, value => existing.NativePopulationMillions = value, imported.NativePopulationMillions);
        changed |= FillMissingLong(() => existing.CaptivePopulationMillions, value => existing.CaptivePopulationMillions = value, imported.CaptivePopulationMillions);
        changed |= FillMissingLong(() => existing.SubjectPopulationMillions, value => existing.SubjectPopulationMillions = value, imported.SubjectPopulationMillions);
        changed |= FillMissingLong(() => existing.IndependentPopulationMillions, value => existing.IndependentPopulationMillions = value, imported.IndependentPopulationMillions);

        if (existing.Founding.Origin == EmpireOrigin.Unknown && imported.Founding.Origin != EmpireOrigin.Unknown)
        {
            existing.Founding.Origin = imported.Founding.Origin;
            changed = true;
        }

        changed |= FillMissingNullable(() => existing.Founding.FoundingWorldId, value => existing.Founding.FoundingWorldId = value, imported.Founding.FoundingWorldId);
        changed |= FillMissingNullable(() => existing.Founding.FoundingColonyId, value => existing.Founding.FoundingColonyId = value, imported.Founding.FoundingColonyId);
        changed |= FillMissingNullable(() => existing.Founding.ParentEmpireId, value => existing.Founding.ParentEmpireId = value, imported.Founding.ParentEmpireId);
        changed |= FillMissingNullable(() => existing.Founding.FoundingRaceId, value => existing.Founding.FoundingRaceId = value, imported.Founding.FoundingRaceId);
        changed |= FillMissingNullable(() => existing.Founding.FoundedCentury, value => existing.Founding.FoundedCentury = value, imported.Founding.FoundedCentury);

        var knownMemberships = existing.RaceMemberships
            .Select(membership => membership.RaceId)
            .ToHashSet();
        foreach (var membership in imported.RaceMemberships.Where(membership => knownMemberships.Add(membership.RaceId)))
        {
            existing.RaceMemberships.Add(new EmpireRaceMembership
            {
                EmpireId = existing.Id,
                RaceId = membership.RaceId,
                Role = membership.Role,
                PopulationMillions = membership.PopulationMillions,
                IsPrimary = membership.IsPrimary
            });
            changed = true;
        }

        var knownContacts = existing.Contacts
            .Select(contact => contact.OtherEmpireId)
            .ToHashSet();
        foreach (var contact in imported.Contacts.Where(contact => knownContacts.Add(contact.OtherEmpireId)))
        {
            existing.Contacts.Add(new EmpireContact
            {
                EmpireId = existing.Id,
                OtherEmpireId = contact.OtherEmpireId,
                Relation = contact.Relation,
                RelationCode = contact.RelationCode,
                Age = contact.Age
            });
            changed = true;
        }

        return changed;
    }

    private static bool MergeMissingColonyData(Colony existing, Colony imported)
    {
        var changed = false;

        changed |= FillMissingString(() => existing.Name, value => existing.Name = value, imported.Name);
        changed |= FillMissingString(() => existing.ColonistRaceName, value => existing.ColonistRaceName = value, imported.ColonistRaceName);
        changed |= FillMissingString(() => existing.AllegianceName, value => existing.AllegianceName = value, imported.AllegianceName);
        changed |= FillMissingString(() => existing.ColonyClass, value => existing.ColonyClass = value, imported.ColonyClass);
        changed |= FillMissingString(() => existing.Starport, value => existing.Starport = value, imported.Starport);
        changed |= FillMissingString(() => existing.GovernmentType, value => existing.GovernmentType = value, imported.GovernmentType);
        changed |= FillMissingString(() => existing.ExportResource, value => existing.ExportResource = value, imported.ExportResource);
        changed |= FillMissingString(() => existing.ImportResource, value => existing.ImportResource = value, imported.ImportResource);

        if (existing.AllegianceId == ushort.MaxValue && imported.AllegianceId != ushort.MaxValue)
        {
            existing.AllegianceId = imported.AllegianceId;
            changed = true;
        }

        if (existing.RaceId == 0 && imported.RaceId > 0)
        {
            existing.RaceId = imported.RaceId;
            changed = true;
        }

        changed |= FillMissingNullable(() => existing.ControllingEmpireId, value => existing.ControllingEmpireId = value, imported.ControllingEmpireId);
        changed |= FillMissingNullable(() => existing.ParentEmpireId, value => existing.ParentEmpireId = value, imported.ParentEmpireId);
        changed |= FillMissingNullable(() => existing.FoundingEmpireId, value => existing.FoundingEmpireId = value, imported.FoundingEmpireId);
        changed |= FillMissingByte(() => existing.EncodedPopulation, value => existing.EncodedPopulation = value, imported.EncodedPopulation);
        changed |= FillMissingLong(() => existing.EstimatedPopulation, value => existing.EstimatedPopulation = value, imported.EstimatedPopulation);
        changed |= FillMissingByte(() => existing.NativePopulationPercent, value => existing.NativePopulationPercent = value, imported.NativePopulationPercent);
        changed |= FillMissingByte(() => existing.ColonyClassCode, value => existing.ColonyClassCode = value, imported.ColonyClassCode);
        changed |= FillMissingByte(() => existing.Crime, value => existing.Crime = value, imported.Crime);
        changed |= FillMissingByte(() => existing.Law, value => existing.Law = value, imported.Law);
        changed |= FillMissingByte(() => existing.Stability, value => existing.Stability = value, imported.Stability);
        changed |= FillMissingByte(() => existing.AgeCenturies, value => existing.AgeCenturies = value, imported.AgeCenturies);
        changed |= FillMissingByte(() => existing.StarportCode, value => existing.StarportCode = value, imported.StarportCode);
        changed |= FillMissingByte(() => existing.GovernmentTypeCode, value => existing.GovernmentTypeCode = value, imported.GovernmentTypeCode);
        changed |= FillMissingUShort(() => existing.GrossWorldProductMcr, value => existing.GrossWorldProductMcr = value, imported.GrossWorldProductMcr);
        changed |= FillMissingUShort(() => existing.MilitaryPower, value => existing.MilitaryPower = value, imported.MilitaryPower);
        changed |= FillMissingByte(() => existing.ExportResourceCode, value => existing.ExportResourceCode = value, imported.ExportResourceCode);
        changed |= FillMissingByte(() => existing.ImportResourceCode, value => existing.ImportResourceCode = value, imported.ImportResourceCode);

        var knownDemographicRaces = existing.Demographics
            .Select(demographic => demographic.RaceId)
            .ToHashSet();
        foreach (var demographic in imported.Demographics.Where(demographic => knownDemographicRaces.Add(demographic.RaceId)))
        {
            existing.Demographics.Add(new ColonyDemographic
            {
                ColonyId = existing.Id,
                RaceId = demographic.RaceId,
                RaceName = demographic.RaceName,
                PopulationPercent = demographic.PopulationPercent,
                Population = demographic.Population
            });
            changed = true;
        }

        changed |= AddMissingStrings(existing.Facilities, imported.Facilities);

        return changed;
    }

    private static bool MergeMissingHistoryData(HistoryEvent existing, HistoryEvent imported)
    {
        var changed = false;

        changed |= FillMissingNullable(() => existing.SectorId, value => existing.SectorId = value, imported.SectorId);
        changed |= FillMissingString(() => existing.EventType, value => existing.EventType = value, imported.EventType);
        changed |= FillMissingNullable(() => existing.RaceId, value => existing.RaceId = value, imported.RaceId);
        changed |= FillMissingNullable(() => existing.OtherRaceId, value => existing.OtherRaceId = value, imported.OtherRaceId);
        changed |= FillMissingNullable(() => existing.EmpireId, value => existing.EmpireId = value, imported.EmpireId);
        changed |= FillMissingNullable(() => existing.ColonyId, value => existing.ColonyId = value, imported.ColonyId);
        changed |= FillMissingNullable(() => existing.PlanetId, value => existing.PlanetId = value, imported.PlanetId);
        changed |= FillMissingNullable(() => existing.StarSystemId, value => existing.StarSystemId = value, imported.StarSystemId);
        changed |= FillMissingString(() => existing.Description, value => existing.Description = value, imported.Description);
        changed |= FillMissingString(() => existing.ImportDataJson, value => existing.ImportDataJson = value, imported.ImportDataJson);

        return changed;
    }

    private static bool FillMissingString(Func<string?> currentValue, Action<string> assignValue, string? importedValue)
    {
        if (string.IsNullOrWhiteSpace(currentValue()) && !string.IsNullOrWhiteSpace(importedValue))
        {
            assignValue(importedValue);
            return true;
        }

        return false;
    }

    private static bool FillMissingNullable<T>(Func<T?> currentValue, Action<T?> assignValue, T? importedValue)
        where T : struct
    {
        if (currentValue() is null && importedValue is not null)
        {
            assignValue(importedValue);
            return true;
        }

        return false;
    }

    private static bool FillMissingByte(Func<byte> currentValue, Action<byte> assignValue, byte importedValue)
    {
        if (currentValue() == 0 && importedValue > 0)
        {
            assignValue(importedValue);
            return true;
        }

        return false;
    }

    private static bool FillMissingUShort(Func<ushort> currentValue, Action<ushort> assignValue, ushort importedValue)
    {
        if (currentValue() == 0 && importedValue > 0)
        {
            assignValue(importedValue);
            return true;
        }

        return false;
    }

    private static bool FillMissingInt(Func<int> currentValue, Action<int> assignValue, int importedValue)
    {
        if (currentValue() == 0 && importedValue > 0)
        {
            assignValue(importedValue);
            return true;
        }

        return false;
    }

    private static bool FillMissingLong(Func<long> currentValue, Action<long> assignValue, long importedValue)
    {
        if (currentValue() == 0 && importedValue > 0)
        {
            assignValue(importedValue);
            return true;
        }

        return false;
    }

    private static bool AddMissingStrings(IList<string> existing, IEnumerable<string> imported)
    {
        var changed = false;
        var knownValues = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var value in imported.Where(value => !string.IsNullOrWhiteSpace(value) && knownValues.Add(value)))
        {
            existing.Add(value);
            changed = true;
        }

        return changed;
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

    private readonly record struct GeneratedNameResult(string Name, bool RequiresUserRename);

    private sealed record LegacyStarSystemImport(
        int LegacyId,
        StarSystem System,
        byte[] PlanetCounts,
        int[] FirstPlanetIds);

    private sealed class StarMapImportBatch
    {
        public List<StarSystem> StarSystems { get; } = [];

        public List<AstralBodyImportRow> AstralBodies { get; } = [];

        public List<World> Worlds { get; } = [];

        public List<UnusualCharacteristicImportRow> UnusualCharacteristics { get; } = [];

        public bool HasChanges =>
            StarSystems.Count > 0
            || AstralBodies.Count > 0
            || Worlds.Count > 0
            || UnusualCharacteristics.Count > 0;
    }

    private sealed record AstralBodyImportRow(int StarSystemId, AstralBody Body);

    private sealed record UnusualCharacteristicImportRow(int WorldId, UnusualCharacteristic Characteristic);

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
