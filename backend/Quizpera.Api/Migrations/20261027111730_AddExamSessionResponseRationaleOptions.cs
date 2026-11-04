using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSessionResponseRationaleOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamSessionResponseRationaleOptions",
                columns: table => new
                {
                    ExamSessionResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionRationaleOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedOrder = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessionResponseRationaleOptions", x => new { x.ExamSessionResponseId, x.QuestionRationaleOptionId });
                    table.ForeignKey(
                        name: "FK_ExamSessionResponseRationaleOptions_ExamSessionResponses_Ex~",
                        column: x => x.ExamSessionResponseId,
                        principalTable: "ExamSessionResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSessionResponseRationaleOptions_QuestionRationaleOption~",
                        column: x => x.QuestionRationaleOptionId,
                        principalTable: "QuestionRationaleOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionResponseRationaleOptions_QuestionRationaleOption~",
                table: "ExamSessionResponseRationaleOptions",
                column: "QuestionRationaleOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSessionResponseRationaleOptions");
        }
    }
}
