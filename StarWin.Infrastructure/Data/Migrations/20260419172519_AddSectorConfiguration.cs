using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SectorConfigurations",
                columns: table => new
                {
                    SectorId = table.Column<int>(type: "int", nullable: false),
                    BasicHyperlaneTierName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BasicHyperlaneMaximumLengthParsecs = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorConfigurations", x => x.SectorId);
                    table.ForeignKey(
                        name: "FK_SectorConfigurations_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
                    ? """
                      INSERT INTO SectorConfigurations (SectorId, BasicHyperlaneTierName, BasicHyperlaneMaximumLengthParsecs, UpdatedAtUtc)
                      SELECT Id, 'Basic', 1.000, CURRENT_TIMESTAMP
                      FROM Sectors
                      WHERE NOT EXISTS (
                          SELECT 1
                          FROM SectorConfigurations
                          WHERE SectorConfigurations.SectorId = Sectors.Id)
                      """
                    : """
                      INSERT INTO SectorConfigurations (SectorId, BasicHyperlaneTierName, BasicHyperlaneMaximumLengthParsecs, UpdatedAtUtc)
                      SELECT Id, 'Basic', 1.000, SYSUTCDATETIME()
                      FROM Sectors
                      WHERE NOT EXISTS (
                          SELECT 1
                          FROM SectorConfigurations
                          WHERE SectorConfigurations.SectorId = Sectors.Id)
                      """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SectorConfigurations");
        }
    }
}
