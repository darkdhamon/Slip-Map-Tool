using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddColonyNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Colonies",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Colonies");
        }
    }
}
