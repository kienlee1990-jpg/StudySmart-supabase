using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyTaiKhoanAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTowerRankDemotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TowerRankAtStart",
                schema: "hoctap",
                table: "QuizAttempts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TowerRankEpochAtStart",
                schema: "hoctap",
                table: "QuizAttempts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TowerRankResult",
                schema: "hoctap",
                table: "QuizAttempts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TowerRanks",
                schema: "hoctap",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    Epoch = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerRanks", x => new { x.UserId, x.Subject });
                    table.ForeignKey(
                        name: "FK_TowerRanks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "hoctap",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TowerRanks",
                schema: "hoctap");

            migrationBuilder.DropColumn(
                name: "TowerRankAtStart",
                schema: "hoctap",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "TowerRankEpochAtStart",
                schema: "hoctap",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "TowerRankResult",
                schema: "hoctap",
                table: "QuizAttempts");
        }
    }
}
