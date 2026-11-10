using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyTaiKhoanAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonTopicAndVideoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Topic",
                schema: "hoctap",
                table: "QuizLessons",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                schema: "hoctap",
                table: "QuizLessons",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Topic",
                schema: "hoctap",
                table: "QuizLessons");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                schema: "hoctap",
                table: "QuizLessons");
        }
    }
}
