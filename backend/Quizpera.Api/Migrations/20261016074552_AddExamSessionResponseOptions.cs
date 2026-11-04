using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSessionResponseOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamSessionResponseOptions",
                columns: table => new
                {
                    ExamSessionResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionOptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessionResponseOptions", x => new { x.ExamSessionResponseId, x.QuestionOptionId });
                    table.ForeignKey(
                        name: "FK_ExamSessionResponseOptions_ExamSessionResponses_ExamSession~",
                        column: x => x.ExamSessionResponseId,
                        principalTable: "ExamSessionResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSessionResponseOptions_QuestionOptions_QuestionOptionId",
                        column: x => x.QuestionOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionResponseOptions_QuestionOptionId",
                table: "ExamSessionResponseOptions",
                column: "QuestionOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSessionResponseOptions");
        }
    }
}
