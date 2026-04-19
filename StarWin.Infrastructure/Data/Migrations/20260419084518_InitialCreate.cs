using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlienRaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    HomePlanetId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EnvironmentType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BodyChemistry = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    GovernmentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BodyCoverType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AppearanceType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Diet = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Reproduction = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReproductionMethod = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Religion = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Devotion = table.Column<byte>(type: "tinyint", nullable: false),
                    DevotionLevel = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    BiologyProfile_PsiPower = table.Column<byte>(type: "tinyint", nullable: false),
                    BiologyProfile_PsiRating = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    BiologyProfile_Body = table.Column<byte>(type: "tinyint", nullable: false),
                    BiologyProfile_Mind = table.Column<byte>(type: "tinyint", nullable: false),
                    BiologyProfile_Speed = table.Column<byte>(type: "tinyint", nullable: false),
                    BiologyProfile_Lifespan = table.Column<byte>(type: "tinyint", nullable: false),
                    MassKg = table.Column<short>(type: "smallint", nullable: false),
                    SizeCm = table.Column<short>(type: "smallint", nullable: false),
                    LimbPairCount = table.Column<byte>(type: "tinyint", nullable: false),
                    LegacyAttributes = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlienRaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    LegacyRaceId = table.Column<int>(type: "int", nullable: true),
                    CivilizationProfile_Militancy = table.Column<byte>(type: "tinyint", nullable: false),
                    CivilizationProfile_Determination = table.Column<byte>(type: "tinyint", nullable: false),
                    CivilizationProfile_RacialTolerance = table.Column<byte>(type: "tinyint", nullable: false),
                    CivilizationProfile_Progressiveness = table.Column<byte>(type: "tinyint", nullable: false),
                    CivilizationProfile_Loyalty = table.Column<byte>(type: "tinyint", nullable: false),
                    CivilizationProfile_SocialCohesion = table.Column<byte>(type: "tinyint", nullable: false),
                    CivilizationProfile_TechLevel = table.Column<byte>(type: "tinyint", nullable: false),
                    CivilizationProfile_Art = table.Column<byte>(type: "tinyint", nullable: false),
                    CivilizationProfile_Individualism = table.Column<byte>(type: "tinyint", nullable: false),
                    CivilizationProfile_SpatialAge = table.Column<byte>(type: "tinyint", nullable: false),
                    Founding_Origin = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Founding_FoundingWorldId = table.Column<int>(type: "int", nullable: true),
                    Founding_FoundingColonyId = table.Column<int>(type: "int", nullable: true),
                    Founding_ParentEmpireId = table.Column<int>(type: "int", nullable: true),
                    Founding_FoundingRaceId = table.Column<int>(type: "int", nullable: true),
                    Founding_FoundedCentury = table.Column<int>(type: "int", nullable: true),
                    ExpansionPolicy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EconomicPowerMcr = table.Column<long>(type: "bigint", nullable: false),
                    MilitaryPower = table.Column<long>(type: "bigint", nullable: false),
                    TradeBonusMcr = table.Column<long>(type: "bigint", nullable: false),
                    Planets = table.Column<int>(type: "int", nullable: false),
                    CaptivePlanets = table.Column<int>(type: "int", nullable: false),
                    Moons = table.Column<int>(type: "int", nullable: false),
                    SubjugatedPlanets = table.Column<int>(type: "int", nullable: false),
                    SubjugatedMoons = table.Column<int>(type: "int", nullable: false),
                    IndependentColonies = table.Column<int>(type: "int", nullable: false),
                    SpaceHabitats = table.Column<int>(type: "int", nullable: false),
                    NativePopulationMillions = table.Column<long>(type: "bigint", nullable: false),
                    CaptivePopulationMillions = table.Column<long>(type: "bigint", nullable: false),
                    SubjectPopulationMillions = table.Column<long>(type: "bigint", nullable: false),
                    IndependentPopulationMillions = table.Column<long>(type: "bigint", nullable: false),
                    MilitaryForces_Personnel_CrewRating = table.Column<byte>(type: "tinyint", nullable: false),
                    MilitaryForces_Personnel_CrewQuality = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MilitaryForces_Personnel_TroopRating = table.Column<byte>(type: "tinyint", nullable: false),
                    MilitaryForces_Personnel_TroopQuality = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MilitaryForces_Personnel_ConscriptionPolicy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MilitaryForces_NavyDoctrine_FighterEmphasisPercent = table.Column<byte>(type: "tinyint", nullable: false),
                    MilitaryForces_NavyDoctrine_MissileEmphasisPercent = table.Column<byte>(type: "tinyint", nullable: false),
                    MilitaryForces_NavyDoctrine_BeamWeaponEmphasisPercent = table.Column<byte>(type: "tinyint", nullable: false),
                    MilitaryForces_NavyDoctrine_AssaultEmphasisPercent = table.Column<byte>(type: "tinyint", nullable: false),
                    MilitaryForces_NavyDoctrine_DefenseEmphasisPercent = table.Column<byte>(type: "tinyint", nullable: false),
                    MilitaryForces_Notes = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empires", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmpireContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpireId = table.Column<int>(type: "int", nullable: false),
                    OtherEmpireId = table.Column<int>(type: "int", nullable: false),
                    Relation = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RelationCode = table.Column<byte>(type: "tinyint", nullable: false),
                    Age = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpireContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmpireContacts_Empires_EmpireId",
                        column: x => x.EmpireId,
                        principalTable: "Empires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmpireFleet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Class = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    MilitaryForceProfileEmpireId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpireFleet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmpireFleet_Empires_MilitaryForceProfileEmpireId",
                        column: x => x.MilitaryForceProfileEmpireId,
                        principalTable: "Empires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmpireGroundForces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    MilitaryForceProfileEmpireId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpireGroundForces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmpireGroundForces_Empires_MilitaryForceProfileEmpireId",
                        column: x => x.MilitaryForceProfileEmpireId,
                        principalTable: "Empires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmpireMilitaryBases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    MilitaryForceProfileEmpireId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpireMilitaryBases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmpireMilitaryBases_Empires_MilitaryForceProfileEmpireId",
                        column: x => x.MilitaryForceProfileEmpireId,
                        principalTable: "Empires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmpireRaceMemberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpireId = table.Column<int>(type: "int", nullable: false),
                    RaceId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PopulationMillions = table.Column<long>(type: "bigint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpireRaceMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmpireRaceMemberships_Empires_EmpireId",
                        column: x => x.EmpireId,
                        principalTable: "Empires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoryEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectorId = table.Column<int>(type: "int", nullable: true),
                    Century = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RaceId = table.Column<int>(type: "int", nullable: true),
                    OtherRaceId = table.Column<int>(type: "int", nullable: true),
                    PlanetId = table.Column<int>(type: "int", nullable: true),
                    StarSystemId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoryEvents_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StarSystems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    SectorId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Coordinates = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    AllegianceId = table.Column<int>(type: "int", nullable: false),
                    MapCode = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarSystems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarSystems_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AstralBodies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ClassificationCode = table.Column<byte>(type: "tinyint", nullable: false),
                    DecimalClassCode = table.Column<byte>(type: "tinyint", nullable: false),
                    PlanetCount = table.Column<byte>(type: "tinyint", nullable: false),
                    Luminosity = table.Column<double>(type: "float", nullable: true),
                    SolarMasses = table.Column<double>(type: "float", nullable: true),
                    CompanionOrbitAu = table.Column<double>(type: "float", nullable: true),
                    StarSystemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AstralBodies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AstralBodies_StarSystems_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpaceHabitats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Population = table.Column<long>(type: "bigint", nullable: false),
                    StarSystemId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    OrbitTargetKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OrbitTargetId = table.Column<int>(type: "int", nullable: false),
                    BuiltByEmpireId = table.Column<int>(type: "int", nullable: true),
                    ControlledByEmpireId = table.Column<int>(type: "int", nullable: true),
                    ConstructedCentury = table.Column<int>(type: "int", nullable: true),
                    OrbitRadiusKm = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceHabitats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpaceHabitats_StarSystems_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Worlds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StarSystemId = table.Column<int>(type: "int", nullable: true),
                    ParentWorldId = table.Column<int>(type: "int", nullable: true),
                    PrimaryAstralBodySequence = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    WorldType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AtmosphereType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AtmosphereComposition = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    WaterType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Hydrography_WaterPercent = table.Column<byte>(type: "tinyint", nullable: false),
                    Hydrography_IcePercent = table.Column<byte>(type: "tinyint", nullable: false),
                    Hydrography_CloudPercent = table.Column<byte>(type: "tinyint", nullable: false),
                    MineralResources_MetalOre = table.Column<byte>(type: "tinyint", nullable: false),
                    MineralResources_RadioactiveOre = table.Column<byte>(type: "tinyint", nullable: false),
                    MineralResources_PreciousMetal = table.Column<byte>(type: "tinyint", nullable: false),
                    MineralResources_RawCrystals = table.Column<byte>(type: "tinyint", nullable: false),
                    MineralResources_PreciousGems = table.Column<byte>(type: "tinyint", nullable: false),
                    DiameterKm = table.Column<int>(type: "int", nullable: false),
                    DensityTenthsEarth = table.Column<byte>(type: "tinyint", nullable: false),
                    AtmosphericPressure = table.Column<float>(type: "real", nullable: false),
                    AverageTemperatureCelsius = table.Column<short>(type: "smallint", nullable: false),
                    MiscellaneousFlags = table.Column<byte>(type: "tinyint", nullable: false),
                    Albedo = table.Column<double>(type: "float", nullable: true),
                    AlienRaceId = table.Column<int>(type: "int", nullable: true),
                    ControlledByEmpireId = table.Column<int>(type: "int", nullable: true),
                    AllegianceId = table.Column<int>(type: "int", nullable: false),
                    OrbitRadiusAu = table.Column<double>(type: "float", nullable: true),
                    OrbitRadiusKm = table.Column<double>(type: "float", nullable: true),
                    SmallestMolecularWeightRetained = table.Column<byte>(type: "tinyint", nullable: true),
                    AxialTiltDegrees = table.Column<byte>(type: "tinyint", nullable: true),
                    OrbitalInclinationDegrees = table.Column<byte>(type: "tinyint", nullable: true),
                    RotationPeriodHours = table.Column<long>(type: "bigint", nullable: true),
                    EccentricityThousandths = table.Column<int>(type: "int", nullable: true),
                    OrbitPeriodDays = table.Column<double>(type: "float", nullable: true),
                    GravityEarthG = table.Column<double>(type: "float", nullable: true),
                    MassEarthMasses = table.Column<double>(type: "float", nullable: true),
                    EscapeVelocityKmPerSecond = table.Column<double>(type: "float", nullable: true),
                    OxygenPressureAtmospheres = table.Column<double>(type: "float", nullable: true),
                    BoilingPointCelsius = table.Column<int>(type: "int", nullable: true),
                    MagneticFieldGauss = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worlds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Worlds_StarSystems_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Colonies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    WorldId = table.Column<int>(type: "int", nullable: false),
                    WorldKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RaceId = table.Column<int>(type: "int", nullable: false),
                    ColonistRaceName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AllegianceId = table.Column<int>(type: "int", nullable: false),
                    AllegianceName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PoliticalStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ControllingEmpireId = table.Column<int>(type: "int", nullable: true),
                    ParentEmpireId = table.Column<int>(type: "int", nullable: true),
                    FoundingEmpireId = table.Column<int>(type: "int", nullable: true),
                    EncodedPopulation = table.Column<byte>(type: "tinyint", nullable: false),
                    EstimatedPopulation = table.Column<long>(type: "bigint", nullable: false),
                    NativePopulationPercent = table.Column<byte>(type: "tinyint", nullable: false),
                    ColonyClass = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ColonyClassCode = table.Column<byte>(type: "tinyint", nullable: false),
                    Crime = table.Column<byte>(type: "tinyint", nullable: false),
                    Law = table.Column<byte>(type: "tinyint", nullable: false),
                    Stability = table.Column<byte>(type: "tinyint", nullable: false),
                    AgeCenturies = table.Column<byte>(type: "tinyint", nullable: false),
                    Starport = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StarportCode = table.Column<byte>(type: "tinyint", nullable: false),
                    GovernmentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GovernmentTypeCode = table.Column<byte>(type: "tinyint", nullable: false),
                    GrossWorldProductMcr = table.Column<int>(type: "int", nullable: false),
                    MilitaryPower = table.Column<int>(type: "int", nullable: false),
                    ExportResource = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ExportResourceCode = table.Column<byte>(type: "tinyint", nullable: false),
                    ImportResource = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ImportResourceCode = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colonies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Colonies_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnusualCharacteristics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    WorldId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnusualCharacteristics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnusualCharacteristics_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AstralBodies_StarSystemId",
                table: "AstralBodies",
                column: "StarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Colonies_WorldId",
                table: "Colonies",
                column: "WorldId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmpireContacts_EmpireId",
                table: "EmpireContacts",
                column: "EmpireId");

            migrationBuilder.CreateIndex(
                name: "IX_EmpireFleet_MilitaryForceProfileEmpireId",
                table: "EmpireFleet",
                column: "MilitaryForceProfileEmpireId");

            migrationBuilder.CreateIndex(
                name: "IX_EmpireGroundForces_MilitaryForceProfileEmpireId",
                table: "EmpireGroundForces",
                column: "MilitaryForceProfileEmpireId");

            migrationBuilder.CreateIndex(
                name: "IX_EmpireMilitaryBases_MilitaryForceProfileEmpireId",
                table: "EmpireMilitaryBases",
                column: "MilitaryForceProfileEmpireId");

            migrationBuilder.CreateIndex(
                name: "IX_EmpireRaceMemberships_EmpireId",
                table: "EmpireRaceMemberships",
                column: "EmpireId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoryEvents_SectorId",
                table: "HistoryEvents",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_Name",
                table: "Sectors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpaceHabitats_StarSystemId",
                table: "SpaceHabitats",
                column: "StarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystems_SectorId_Name",
                table: "StarSystems",
                columns: new[] { "SectorId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_UnusualCharacteristics_WorldId",
                table: "UnusualCharacteristics",
                column: "WorldId");

            migrationBuilder.CreateIndex(
                name: "IX_Worlds_StarSystemId",
                table: "Worlds",
                column: "StarSystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlienRaces");

            migrationBuilder.DropTable(
                name: "AstralBodies");

            migrationBuilder.DropTable(
                name: "Colonies");

            migrationBuilder.DropTable(
                name: "EmpireContacts");

            migrationBuilder.DropTable(
                name: "EmpireFleet");

            migrationBuilder.DropTable(
                name: "EmpireGroundForces");

            migrationBuilder.DropTable(
                name: "EmpireMilitaryBases");

            migrationBuilder.DropTable(
                name: "EmpireRaceMemberships");

            migrationBuilder.DropTable(
                name: "HistoryEvents");

            migrationBuilder.DropTable(
                name: "SpaceHabitats");

            migrationBuilder.DropTable(
                name: "UnusualCharacteristics");

            migrationBuilder.DropTable(
                name: "Empires");

            migrationBuilder.DropTable(
                name: "Worlds");

            migrationBuilder.DropTable(
                name: "StarSystems");

            migrationBuilder.DropTable(
                name: "Sectors");
        }
    }
}
