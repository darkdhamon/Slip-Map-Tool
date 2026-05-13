using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorEmpireStatsCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SectorEmpireStatsCalculatedAtUtc",
                table: "SectorConfigurations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SectorEmpireStatsInvalidatedAtUtc",
                table: "SectorConfigurations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SectorEmpireStats",
                columns: table => new
                {
                    SectorId = table.Column<int>(type: "int", nullable: false),
                    EmpireId = table.Column<int>(type: "int", nullable: false),
                    ControlledWorldCount = table.Column<int>(type: "int", nullable: false),
                    TrackedWorldCount = table.Column<int>(type: "int", nullable: false),
                    LastCalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorEmpireStats", x => new { x.SectorId, x.EmpireId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_SectorEmpireStats_EmpireId_SectorId",
                table: "SectorEmpireStats",
                columns: new[] { "EmpireId", "SectorId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SectorEmpireStats");

            migrationBuilder.DropColumn(
                name: "SectorEmpireStatsCalculatedAtUtc",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "SectorEmpireStatsInvalidatedAtUtc",
                table: "SectorConfigurations");
        }
    }
}
