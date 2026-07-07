using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BestOf",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "CountType",
                table: "Tournaments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GamesToWinSet",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<int>(
                name: "MinDiff",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "NoAdv",
                table: "Tournaments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BestOf",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "CountType",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "GamesToWinSet",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "MinDiff",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "NoAdv",
                table: "Tournaments");
        }
    }
}
