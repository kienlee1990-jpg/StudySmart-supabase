using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyTaiKhoanAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizQuestionDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                schema: "hoctap",
                table: "QuizQuestions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Thường");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difficulty",
                schema: "hoctap",
                table: "QuizQuestions");
        }
    }
}
