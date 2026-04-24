using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHyperlaneConnectionLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdditionalCrossEmpireConnectionsPerSystem",
                table: "SectorConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Tl9AndBelowMaximumConnectionsPerSystem",
                table: "SectorConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalCrossEmpireConnectionsPerSystem",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl9AndBelowMaximumConnectionsPerSystem",
                table: "SectorConfigurations");
        }
    }
}
