using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teams_TournamentId_Name",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "Player1",
                table: "Teams",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                unicode: true);

            migrationBuilder.AddColumn<string>(
                name: "Player2",
                table: "Teams",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                unicode: true);

            // Copy existing Name data: split on '/' into Player1 / Player2
            migrationBuilder.Sql(@"
                UPDATE Teams
                SET Player1 = CASE
                        WHEN CHARINDEX('/', Name) > 0
                        THEN LEFT(Name, CHARINDEX('/', Name) - 1)
                        ELSE Name
                    END,
                    Player2 = CASE
                        WHEN CHARINDEX('/', Name) > 0
                        THEN NULLIF(LTRIM(RTRIM(SUBSTRING(Name, CHARINDEX('/', Name) + 1, 64))), '')
                        ELSE NULL
                    END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Player1",
                table: "Teams",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                unicode: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldUnicode: true);

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Teams");

            migrationBuilder.CreateTable(
                name: "Sets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    No = table.Column<int>(type: "int", nullable: false),
                    ScoreA = table.Column<int>(type: "int", nullable: false),
                    ScoreB = table.Column<int>(type: "int", nullable: false),
                    TieBreakPoints = table.Column<int>(type: "int", nullable: true),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sets_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    No = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Server = table.Column<int>(type: "int", nullable: true),
                    SetId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Sets_SetId",
                        column: x => x.SetId,
                        principalTable: "Sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TournamentId_Player1_Player2",
                table: "Teams",
                columns: new[] { "TournamentId", "Player1", "Player2" },
                unique: true,
                filter: "[Player2] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Games_SetId_No",
                table: "Games",
                columns: new[] { "SetId", "No" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sets_MatchId_No",
                table: "Sets",
                columns: new[] { "MatchId", "No" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Sets");

            migrationBuilder.DropIndex(
                name: "IX_Teams_TournamentId_Player1_Player2",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Teams",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                unicode: true);

            // Restore Name from Player1 / Player2
            migrationBuilder.Sql(@"
                UPDATE Teams
                SET Name = CASE
                        WHEN Player2 IS NOT NULL THEN Player1 + '/' + Player2
                        ELSE Player1
                    END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Teams",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                unicode: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldUnicode: true);

            migrationBuilder.DropColumn(name: "Player1", table: "Teams");
            migrationBuilder.DropColumn(name: "Player2", table: "Teams");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TournamentId_Name",
                table: "Teams",
                columns: new[] { "TournamentId", "Name" },
                unique: true);
        }
    }
}
