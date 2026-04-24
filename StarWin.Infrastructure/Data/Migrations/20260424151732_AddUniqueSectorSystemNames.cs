using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSectorSystemNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StarSystems_SectorId_Name",
                table: "StarSystems");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystems_SectorId_Name",
                table: "StarSystems",
                columns: new[] { "SectorId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StarSystems_SectorId_Name",
                table: "StarSystems");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystems_SectorId_Name",
                table: "StarSystems",
                columns: new[] { "SectorId", "Name" });
        }
    }
}
