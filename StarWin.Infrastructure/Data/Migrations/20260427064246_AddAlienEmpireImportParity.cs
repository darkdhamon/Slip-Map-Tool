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
            migrationBuilder.DropColumn(
                name: "Religion",
                table: "AlienRaces");

            migrationBuilder.RenameColumn(
                name: "GovernmentType",
                table: "AlienRaces",
                newName: "HairType");

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
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Abilities",
                table: "AlienRaces",
                type: "nvarchar(max)",
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
                type: "nvarchar(max)",
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
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EyeCharacteristics",
                table: "AlienRaces",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EyeColors",
                table: "AlienRaces",
                type: "nvarchar(max)",
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
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImportDataJson",
                table: "AlienRaces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LimbTypes",
                table: "AlienRaces",
                type: "nvarchar(max)",
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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

            migrationBuilder.RenameColumn(
                name: "HairType",
                table: "AlienRaces",
                newName: "GovernmentType");

            migrationBuilder.AddColumn<string>(
                name: "Religion",
                table: "AlienRaces",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");
        }
    }
}
