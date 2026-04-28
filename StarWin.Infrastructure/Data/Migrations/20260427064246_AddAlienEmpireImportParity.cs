using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlienEmpireImportParity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CivilizationModifiers_Art",
                table: "Empires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CivilizationModifiers_Determination",
                table: "Empires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CivilizationModifiers_Individualism",
                table: "Empires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CivilizationModifiers_Loyalty",
                table: "Empires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CivilizationModifiers_Militancy",
                table: "Empires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CivilizationModifiers_Progressiveness",
                table: "Empires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CivilizationModifiers_RacialTolerance",
                table: "Empires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CivilizationModifiers_SocialCohesion",
                table: "Empires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GovernmentType",
                table: "Empires",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImportDataJson",
                table: "Empires",
                type: UnboundedTextType(migrationBuilder),
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Abilities",
                table: "AlienRaces",
                type: UnboundedTextType(migrationBuilder),
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AtmosphereBreathed",
                table: "AlienRaces",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BodyCharacteristics",
                table: "AlienRaces",
                type: UnboundedTextType(migrationBuilder),
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte>(
                name: "CivilizationProfile_Art",
                table: "AlienRaces",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "CivilizationProfile_Determination",
                table: "AlienRaces",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "CivilizationProfile_Individualism",
                table: "AlienRaces",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "CivilizationProfile_Loyalty",
                table: "AlienRaces",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "CivilizationProfile_Militancy",
                table: "AlienRaces",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "CivilizationProfile_Progressiveness",
                table: "AlienRaces",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "CivilizationProfile_RacialTolerance",
                table: "AlienRaces",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "CivilizationProfile_SocialCohesion",
                table: "AlienRaces",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "ColorPattern",
                table: "AlienRaces",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Colors",
                table: "AlienRaces",
                type: UnboundedTextType(migrationBuilder),
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EyeCharacteristics",
                table: "AlienRaces",
                type: UnboundedTextType(migrationBuilder),
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EyeColors",
                table: "AlienRaces",
                type: UnboundedTextType(migrationBuilder),
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GravityPreference",
                table: "AlienRaces",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HairColors",
                table: "AlienRaces",
                type: UnboundedTextType(migrationBuilder),
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HairType",
                table: "AlienRaces",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImportDataJson",
                table: "AlienRaces",
                type: UnboundedTextType(migrationBuilder),
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LimbTypes",
                table: "AlienRaces",
                type: UnboundedTextType(migrationBuilder),
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresUserRename",
                table: "AlienRaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TemperaturePreference",
                table: "AlienRaces",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Religions",
                columns: table => new
                {
                    Id = table.Column<int>(type: IntegerKeyType(migrationBuilder), nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsUserDefined = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Religions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmpireReligions",
                columns: table => new
                {
                    EmpireId = table.Column<int>(type: "int", nullable: false),
                    ReligionId = table.Column<int>(type: "int", nullable: false),
                    ReligionName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PopulationPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpireReligions", x => new { x.EmpireId, x.ReligionId });
                    table.ForeignKey(
                        name: "FK_EmpireReligions_Empires_EmpireId",
                        column: x => x.EmpireId,
                        principalTable: "Empires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmpireReligions_Religions_ReligionId",
                        column: x => x.ReligionId,
                        principalTable: "Religions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmpireReligions_ReligionId",
                table: "EmpireReligions",
                column: "ReligionId");

            migrationBuilder.CreateIndex(
                name: "IX_Religions_Name",
                table: "Religions",
                column: "Name",
                unique: true);

            NormalizeLegacyEnumLabels(migrationBuilder);
            BackfillLegacyGovernmentTypes(migrationBuilder);
            BackfillLegacyReligions(migrationBuilder);

            migrationBuilder.DropColumn(
                name: "GovernmentType",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "Religion",
                table: "AlienRaces");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmpireReligions");

            migrationBuilder.DropTable(
                name: "Religions");

            migrationBuilder.DropColumn(
                name: "CivilizationModifiers_Art",
                table: "Empires");

            migrationBuilder.DropColumn(
                name: "CivilizationModifiers_Determination",
                table: "Empires");

            migrationBuilder.DropColumn(
                name: "CivilizationModifiers_Individualism",
                table: "Empires");

            migrationBuilder.DropColumn(
                name: "CivilizationModifiers_Loyalty",
                table: "Empires");

            migrationBuilder.DropColumn(
                name: "CivilizationModifiers_Militancy",
                table: "Empires");

            migrationBuilder.DropColumn(
                name: "CivilizationModifiers_Progressiveness",
                table: "Empires");

            migrationBuilder.DropColumn(
                name: "CivilizationModifiers_RacialTolerance",
                table: "Empires");

            migrationBuilder.DropColumn(
                name: "CivilizationModifiers_SocialCohesion",
                table: "Empires");

            migrationBuilder.DropColumn(
                name: "GovernmentType",
                table: "Empires");

            migrationBuilder.DropColumn(
                name: "ImportDataJson",
                table: "Empires");

            migrationBuilder.DropColumn(
                name: "Abilities",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "AtmosphereBreathed",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "BodyCharacteristics",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "CivilizationProfile_Art",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "CivilizationProfile_Determination",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "CivilizationProfile_Individualism",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "CivilizationProfile_Loyalty",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "CivilizationProfile_Militancy",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "CivilizationProfile_Progressiveness",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "CivilizationProfile_RacialTolerance",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "CivilizationProfile_SocialCohesion",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "ColorPattern",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "Colors",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "EyeCharacteristics",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "EyeColors",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "GravityPreference",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "HairColors",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "HairType",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "ImportDataJson",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "LimbTypes",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "RequiresUserRename",
                table: "AlienRaces");

            migrationBuilder.DropColumn(
                name: "TemperaturePreference",
                table: "AlienRaces");

            migrationBuilder.AddColumn<string>(
                name: "GovernmentType",
                table: "AlienRaces",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Religion",
                table: "AlienRaces",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");
        }

        private static string UnboundedTextType(MigrationBuilder migrationBuilder)
        {
            return migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
                ? "TEXT"
                : "nvarchar(max)";
        }

        private static string IntegerKeyType(MigrationBuilder migrationBuilder)
        {
            return migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
                ? "INTEGER"
                : "int";
        }

        private static void BackfillLegacyGovernmentTypes(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE "Empires"
                    SET "GovernmentType" = COALESCE(NULLIF("GovernmentType", ''), (
                        SELECT "GovernmentType"
                        FROM "AlienRaces"
                        WHERE "AlienRaces"."Id" = "Empires"."LegacyRaceId"
                    ))
                    WHERE ("GovernmentType" IS NULL OR TRIM("GovernmentType") = '')
                      AND "LegacyRaceId" IS NOT NULL;
                    """);

                return;
            }

            migrationBuilder.Sql(
                """
                UPDATE empire
                SET empire.GovernmentType = race.GovernmentType
                FROM Empires AS empire
                INNER JOIN AlienRaces AS race ON race.Id = empire.LegacyRaceId
                WHERE (empire.GovernmentType IS NULL OR LTRIM(RTRIM(empire.GovernmentType)) = '')
                  AND race.GovernmentType IS NOT NULL
                  AND LTRIM(RTRIM(race.GovernmentType)) <> '';
                """);
        }

        private static void NormalizeLegacyEnumLabels(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE AlienRaces
                SET DevotionLevel = CASE TRIM(DevotionLevel)
                    WHEN 'Moderate' THEN 'Fair'
                    WHEN 'Low' THEN 'Poor'
                    ELSE DevotionLevel
                END,
                BiologyProfile_PsiRating = CASE TRIM(BiologyProfile_PsiRating)
                    WHEN 'Very Poor' THEN 'VeryPoor'
                    ELSE BiologyProfile_PsiRating
                END
                WHERE DevotionLevel IN ('Moderate', 'Low')
                   OR BiologyProfile_PsiRating = 'Very Poor';
                """);
        }

        private static void BackfillLegacyReligions(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
                    INSERT OR IGNORE INTO "Religions" ("Name", "Type", "IsUserDefined")
                    SELECT DISTINCT TRIM("Religion"), TRIM("Religion"), 0
                    FROM "AlienRaces"
                    WHERE "Religion" IS NOT NULL AND TRIM("Religion") <> '';
                    """);

                migrationBuilder.Sql(
                    """
                    INSERT OR IGNORE INTO "EmpireReligions" ("EmpireId", "ReligionId", "ReligionName", "PopulationPercent")
                    SELECT empire."Id", religion."Id", religion."Name", 100
                    FROM "Empires" AS empire
                    INNER JOIN "AlienRaces" AS race ON race."Id" = empire."LegacyRaceId"
                    INNER JOIN "Religions" AS religion ON religion."Name" = TRIM(race."Religion")
                    WHERE race."Religion" IS NOT NULL AND TRIM(race."Religion") <> '';
                    """);

                return;
            }

            migrationBuilder.Sql(
                """
                INSERT INTO Religions (Name, Type, IsUserDefined)
                SELECT DISTINCT LTRIM(RTRIM(race.Religion)), LTRIM(RTRIM(race.Religion)), CAST(0 AS bit)
                FROM AlienRaces AS race
                WHERE race.Religion IS NOT NULL
                  AND LTRIM(RTRIM(race.Religion)) <> ''
                  AND NOT EXISTS (
                      SELECT 1
                      FROM Religions AS existing
                      WHERE existing.Name = LTRIM(RTRIM(race.Religion))
                  );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO EmpireReligions (EmpireId, ReligionId, ReligionName, PopulationPercent)
                SELECT empire.Id, religion.Id, religion.Name, CAST(100 AS decimal(18,2))
                FROM Empires AS empire
                INNER JOIN AlienRaces AS race ON race.Id = empire.LegacyRaceId
                INNER JOIN Religions AS religion ON religion.Name = LTRIM(RTRIM(race.Religion))
                WHERE race.Religion IS NOT NULL
                  AND LTRIM(RTRIM(race.Religion)) <> ''
                  AND NOT EXISTS (
                      SELECT 1
                      FROM EmpireReligions AS existing
                      WHERE existing.EmpireId = empire.Id
                        AND existing.ReligionId = religion.Id
                  );
                """);
        }
    }
}
