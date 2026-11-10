using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyTaiKhoanAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizQuestionTopic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Topic",
                schema: "hoctap",
                table: "QuizQuestions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Topic",
                schema: "hoctap",
                table: "QuizQuestions");
        }
    }
}
