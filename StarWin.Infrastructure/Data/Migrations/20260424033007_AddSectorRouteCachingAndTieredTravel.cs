using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorRouteCachingAndTieredTravel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OffLaneMaximumDistanceParsecs",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 2m);

            migrationBuilder.AddColumn<string>(
                name: "Tl10HyperlaneName",
                table: "SectorConfigurations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Ascendant Hyperlane");

            migrationBuilder.AddColumn<decimal>(
                name: "Tl10HyperlaneSpeedModifier",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 3m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tl10MaximumDistanceParsecs",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: -1m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tl10OffLaneSpeedMultiplier",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 32m);

            migrationBuilder.AddColumn<string>(
                name: "Tl6HyperlaneName",
                table: "SectorConfigurations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Basic Hyperlane");

            migrationBuilder.AddColumn<decimal>(
                name: "Tl6HyperlaneSpeedModifier",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 2m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tl6MaximumDistanceParsecs",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tl6OffLaneSpeedMultiplier",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 2m);

            migrationBuilder.AddColumn<string>(
                name: "Tl7HyperlaneName",
                table: "SectorConfigurations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Enhanced Hyperlane");

            migrationBuilder.AddColumn<decimal>(
                name: "Tl7HyperlaneSpeedModifier",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 2.25m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tl7MaximumDistanceParsecs",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 1.2m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tl7OffLaneSpeedMultiplier",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 4m);

            migrationBuilder.AddColumn<string>(
                name: "Tl8HyperlaneName",
                table: "SectorConfigurations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Advanced Hyperlane");

            migrationBuilder.AddColumn<decimal>(
                name: "Tl8HyperlaneSpeedModifier",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 2.5m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tl8MaximumDistanceParsecs",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 1.4m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tl8OffLaneSpeedMultiplier",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 8m);

            migrationBuilder.AddColumn<string>(
                name: "Tl9HyperlaneName",
                table: "SectorConfigurations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Prime Hyperlane");

            migrationBuilder.AddColumn<decimal>(
                name: "Tl9HyperlaneSpeedModifier",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 2.75m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tl9MaximumDistanceParsecs",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 1.6m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tl9OffLaneSpeedMultiplier",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 16m);

            migrationBuilder.CreateTable(
                name: "SectorSavedRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectorId = table.Column<int>(type: "int", nullable: false),
                    SourceSystemId = table.Column<int>(type: "int", nullable: false),
                    TargetSystemId = table.Column<int>(type: "int", nullable: false),
                    DistanceParsecs = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    TravelTimeYears = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: false),
                    TechnologyLevel = table.Column<byte>(type: "tinyint", nullable: false),
                    TierName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PrimaryOwnerEmpireId = table.Column<int>(type: "int", nullable: true),
                    PrimaryOwnerEmpireName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SecondaryOwnerEmpireId = table.Column<int>(type: "int", nullable: true),
                    SecondaryOwnerEmpireName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorSavedRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectorSavedRoutes_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SectorSavedRoutes_SectorId_SourceSystemId_TargetSystemId",
                table: "SectorSavedRoutes",
                columns: new[] { "SectorId", "SourceSystemId", "TargetSystemId" },
                unique: true);

            migrationBuilder.Sql(
                """
                UPDATE SectorConfigurations
                SET Tl6MaximumDistanceParsecs = BasicHyperlaneMaximumLengthParsecs,
                    Tl7MaximumDistanceParsecs = OwnedHyperlaneBaseMaximumLengthParsecs
                """);

            migrationBuilder.DropColumn(
                name: "BasicHyperlaneMaximumLengthParsecs",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "OwnedHyperlaneBaseMaximumLengthParsecs",
                table: "SectorConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SectorSavedRoutes");

            migrationBuilder.AddColumn<decimal>(
                name: "BasicHyperlaneMaximumLengthParsecs",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "OwnedHyperlaneBaseMaximumLengthParsecs",
                table: "SectorConfigurations",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 1.2m);

            migrationBuilder.Sql(
                """
                UPDATE SectorConfigurations
                SET BasicHyperlaneMaximumLengthParsecs = Tl6MaximumDistanceParsecs,
                    OwnedHyperlaneBaseMaximumLengthParsecs = Tl7MaximumDistanceParsecs
                """);

            migrationBuilder.DropColumn(
                name: "OffLaneMaximumDistanceParsecs",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl10HyperlaneName",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl10HyperlaneSpeedModifier",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl10MaximumDistanceParsecs",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl10OffLaneSpeedMultiplier",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl6HyperlaneName",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl6HyperlaneSpeedModifier",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl6MaximumDistanceParsecs",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl6OffLaneSpeedMultiplier",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl7HyperlaneName",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl7HyperlaneSpeedModifier",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl7MaximumDistanceParsecs",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl7OffLaneSpeedMultiplier",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl8HyperlaneName",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl8HyperlaneSpeedModifier",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl8MaximumDistanceParsecs",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl8OffLaneSpeedMultiplier",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl9HyperlaneName",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl9HyperlaneSpeedModifier",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl9MaximumDistanceParsecs",
                table: "SectorConfigurations");

            migrationBuilder.DropColumn(
                name: "Tl9OffLaneSpeedMultiplier",
                table: "SectorConfigurations");
        }
    }
}
