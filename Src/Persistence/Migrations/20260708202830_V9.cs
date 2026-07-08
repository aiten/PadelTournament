using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MinDiff",
                table: "Formats",
                newName: "MinMargin");

            migrationBuilder.AddColumn<bool>(
                name: "NoTiebreak",
                table: "Formats",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoTiebreak",
                table: "Formats");

            migrationBuilder.RenameColumn(
                name: "MinMargin",
                table: "Formats",
                newName: "MinDiff");
        }
    }
}
