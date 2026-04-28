using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpireIsFallenFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFallen",
                table: "Empires",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Colonies_ControllingEmpireId",
                table: "Colonies",
                column: "ControllingEmpireId");

            migrationBuilder.CreateIndex(
                name: "IX_Colonies_FoundingEmpireId",
                table: "Colonies",
                column: "FoundingEmpireId");

            migrationBuilder.CreateIndex(
                name: "IX_EmpireRaceMemberships_RaceId_EmpireId",
                table: "EmpireRaceMemberships",
                columns: new[] { "RaceId", "EmpireId" });

            migrationBuilder.CreateIndex(
                name: "IX_Empires_Name",
                table: "Empires",
                column: "Name");

            BackfillStoredFallenEmpireFlags(migrationBuilder);

            migrationBuilder.CreateIndex(
                name: "IX_Empires_IsFallen",
                table: "Empires",
                column: "IsFallen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Empires_IsFallen",
                table: "Empires");

            migrationBuilder.DropIndex(
                name: "IX_Empires_Name",
                table: "Empires");

            migrationBuilder.DropIndex(
                name: "IX_EmpireRaceMemberships_RaceId_EmpireId",
                table: "EmpireRaceMemberships");

            migrationBuilder.DropIndex(
                name: "IX_Colonies_ControllingEmpireId",
                table: "Colonies");

            migrationBuilder.DropIndex(
                name: "IX_Colonies_FoundingEmpireId",
                table: "Colonies");

            migrationBuilder.DropColumn(
                name: "IsFallen",
                table: "Empires");
        }

        private static void BackfillStoredFallenEmpireFlags(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE "Empires"
                    SET "IsFallen" = CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM "Colonies" AS colony
                            WHERE colony."ControllingEmpireId" = "Empires"."Id")
                            THEN 0
                        WHEN EXISTS (
                            SELECT 1
                            FROM "Colonies" AS colony
                            WHERE colony."ControllingEmpireId" = "Empires"."Id"
                               OR colony."FoundingEmpireId" = "Empires"."Id")
                            OR COALESCE("Planets", 0) > 0
                            OR COALESCE("Moons", 0) > 0
                            OR COALESCE("SpaceHabitats", 0) > 0
                            OR COALESCE("NativePopulationMillions", 0) > 0
                            OR COALESCE("SubjectPopulationMillions", 0) > 0
                            THEN 1
                        ELSE 0
                    END;
                    """);

                return;
            }

            migrationBuilder.Sql(
                """
                UPDATE empire
                SET empire.IsFallen = CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM Colonies AS colony
                        WHERE colony.ControllingEmpireId = empire.Id)
                        THEN CAST(0 AS bit)
                    WHEN EXISTS (
                        SELECT 1
                        FROM Colonies AS colony
                        WHERE colony.ControllingEmpireId = empire.Id
                           OR colony.FoundingEmpireId = empire.Id)
                        OR ISNULL(empire.Planets, 0) > 0
                        OR ISNULL(empire.Moons, 0) > 0
                        OR ISNULL(empire.SpaceHabitats, 0) > 0
                        OR ISNULL(empire.NativePopulationMillions, 0) > 0
                        OR ISNULL(empire.SubjectPopulationMillions, 0) > 0
                        THEN CAST(1 AS bit)
                    ELSE CAST(0 AS bit)
                END
                FROM Empires AS empire;
                """);
        }
    }
}
