using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueExamSessionQuestionResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamSessionResponses_ExamSessionId",
                table: "ExamSessionResponses");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionResponses_ExamSessionId_QuestionId",
                table: "ExamSessionResponses",
                columns: new[] { "ExamSessionId", "QuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamSessionResponses_ExamSessionId_QuestionId",
                table: "ExamSessionResponses");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionResponses_ExamSessionId",
                table: "ExamSessionResponses",
                column: "ExamSessionId");
        }
    }
}
