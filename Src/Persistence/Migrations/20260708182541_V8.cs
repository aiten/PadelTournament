using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BestOf",
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

            migrationBuilder.RenameColumn(
                name: "Format",
                table: "Tournaments",
                newName: "FormatId");

            migrationBuilder.CreateTable(
                name: "Formats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlayingFormat = table.Column<int>(type: "int", nullable: false),
                    BestOf = table.Column<int>(type: "int", nullable: true),
                    GamesToWinSet = table.Column<int>(type: "int", nullable: true),
                    MinDiff = table.Column<int>(type: "int", nullable: true),
                    NoAdv = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_FormatId",
                table: "Tournaments",
                column: "FormatId");

            migrationBuilder.CreateIndex(
                name: "IX_Formats_Name",
                table: "Formats",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Formats_FormatId",
                table: "Tournaments",
                column: "FormatId",
                principalTable: "Formats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Formats_FormatId",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "Formats");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_FormatId",
                table: "Tournaments");

            migrationBuilder.RenameColumn(
                name: "FormatId",
                table: "Tournaments",
                newName: "Format");

            migrationBuilder.AddColumn<int>(
                name: "BestOf",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesToWinSet",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinDiff",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "NoAdv",
                table: "Tournaments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
