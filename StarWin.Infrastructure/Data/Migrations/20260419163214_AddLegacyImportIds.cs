using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyImportIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Worlds_StarSystemId",
                table: "Worlds");

            migrationBuilder.AddColumn<int>(
                name: "LegacyMoonId",
                table: "Worlds",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LegacyPlanetId",
                table: "Worlds",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LegacySystemId",
                table: "StarSystems",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE StarSystems
                SET LegacySystemId = Id - (SectorId * 100000) - 1
                WHERE Id >= (SectorId * 100000) + 1
                    AND Id < (SectorId * 100000) + 100000;

                UPDATE Worlds
                SET LegacyPlanetId = Worlds.Id - (StarSystems.SectorId * 1000000) - 1
                FROM Worlds
                INNER JOIN StarSystems ON Worlds.StarSystemId = StarSystems.Id
                WHERE Worlds.Kind = 'Planet'
                    AND Worlds.Id >= (StarSystems.SectorId * 1000000) + 1
                    AND Worlds.Id < (StarSystems.SectorId * 1000000) + 500000;

                UPDATE Worlds
                SET LegacyMoonId = Worlds.Id - (StarSystems.SectorId * 1000000) - 500000 - 1
                FROM Worlds
                INNER JOIN StarSystems ON Worlds.StarSystemId = StarSystems.Id
                WHERE Worlds.Kind = 'Moon'
                    AND Worlds.Id >= (StarSystems.SectorId * 1000000) + 500001
                    AND Worlds.Id < (StarSystems.SectorId * 1000000) + 1000000;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Worlds_StarSystemId_LegacyMoonId",
                table: "Worlds",
                columns: new[] { "StarSystemId", "LegacyMoonId" });

            migrationBuilder.CreateIndex(
                name: "IX_Worlds_StarSystemId_LegacyPlanetId",
                table: "Worlds",
                columns: new[] { "StarSystemId", "LegacyPlanetId" });

            migrationBuilder.CreateIndex(
                name: "IX_StarSystems_SectorId_LegacySystemId",
                table: "StarSystems",
                columns: new[] { "SectorId", "LegacySystemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Worlds_StarSystemId_LegacyMoonId",
                table: "Worlds");

            migrationBuilder.DropIndex(
                name: "IX_Worlds_StarSystemId_LegacyPlanetId",
                table: "Worlds");

            migrationBuilder.DropIndex(
                name: "IX_StarSystems_SectorId_LegacySystemId",
                table: "StarSystems");

            migrationBuilder.DropColumn(
                name: "LegacyMoonId",
                table: "Worlds");

            migrationBuilder.DropColumn(
                name: "LegacyPlanetId",
                table: "Worlds");

            migrationBuilder.DropColumn(
                name: "LegacySystemId",
                table: "StarSystems");

            migrationBuilder.CreateIndex(
                name: "IX_Worlds_StarSystemId",
                table: "Worlds",
                column: "StarSystemId");
        }
    }
}
