using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWin.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntityNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    Markdown = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityNotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityNotes_TargetKind_TargetId",
                table: "EntityNotes",
                columns: new[] { "TargetKind", "TargetId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityNotes");
        }
    }
}
