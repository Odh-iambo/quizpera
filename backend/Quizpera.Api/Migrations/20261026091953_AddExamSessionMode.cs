using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSessionMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "ExamSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ExamSessions");
        }
    }
}
