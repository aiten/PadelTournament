using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RegistrationPin",
                table: "Tournaments",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationCode",
                table: "Teams",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_RegistrationPin",
                table: "Tournaments",
                column: "RegistrationPin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TournamentId_RegistrationCode",
                table: "Teams",
                columns: new[] { "TournamentId", "RegistrationCode" },
                unique: true);

            migrationBuilder.Sql(@"
                UPDATE Tournaments
                SET RegistrationPin = RegistrationPin + '00'
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tournaments_RegistrationPin",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Teams_TournamentId_RegistrationCode",
                table: "Teams");

            migrationBuilder.AlterColumn<int>(
                name: "RegistrationPin",
                table: "Tournaments",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationCode",
                table: "Teams",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);
        }
    }
}
