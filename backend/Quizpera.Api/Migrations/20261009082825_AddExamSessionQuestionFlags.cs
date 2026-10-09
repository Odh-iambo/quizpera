using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSessionQuestionFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamSessionQuestionFlags",
                columns: table => new
                {
                    ExamSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlaggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessionQuestionFlags", x => new { x.ExamSessionId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_ExamSessionQuestionFlags_ExamSessions_ExamSessionId",
                        column: x => x.ExamSessionId,
                        principalTable: "ExamSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSessionQuestionFlags_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionQuestionFlags_QuestionId",
                table: "ExamSessionQuestionFlags",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSessionQuestionFlags");
        }
    }
}
