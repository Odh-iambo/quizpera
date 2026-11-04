using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionOptionCorrectOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrectOrder",
                table: "QuestionOptions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectOrder",
                table: "QuestionOptions");
        }
    }
}
