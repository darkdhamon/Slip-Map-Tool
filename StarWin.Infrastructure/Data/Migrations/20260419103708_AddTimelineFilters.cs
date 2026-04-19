using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimelineFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColonyId",
                table: "HistoryEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmpireId",
                table: "HistoryEvents",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColonyId",
                table: "HistoryEvents");

            migrationBuilder.DropColumn(
                name: "EmpireId",
                table: "HistoryEvents");
        }
    }
}
