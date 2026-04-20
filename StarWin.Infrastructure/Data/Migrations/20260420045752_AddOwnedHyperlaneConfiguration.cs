using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnedHyperlaneConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OwnedHyperlaneBaseMaximumLengthParsecs",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 1.2m);

            migrationBuilder.Sql("""
                UPDATE SectorConfigurations
                SET OwnedHyperlaneBaseMaximumLengthParsecs = BasicHyperlaneMaximumLengthParsecs * 1.2
                WHERE OwnedHyperlaneBaseMaximumLengthParsecs <= 0
                """);

            migrationBuilder.DropColumn(
                name: "BasicHyperlaneTierName",
                table: "SectorConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnedHyperlaneBaseMaximumLengthParsecs",
                table: "SectorConfigurations");

            migrationBuilder.AddColumn<string>(
                name: "BasicHyperlaneTierName",
                table: "SectorConfigurations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");
        }
    }
}
