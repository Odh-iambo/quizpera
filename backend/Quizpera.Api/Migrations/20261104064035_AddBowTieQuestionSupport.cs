using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBowTieQuestionSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BowTieOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BowTieOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BowTieOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamSessionResponseBowTieSelections",
                columns: table => new
                {
                    ExamSessionResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    BowTieOptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessionResponseBowTieSelections", x => new { x.ExamSessionResponseId, x.BowTieOptionId });
                    table.ForeignKey(
                        name: "FK_ExamSessionResponseBowTieSelections_BowTieOptions_BowTieOpt~",
                        column: x => x.BowTieOptionId,
                        principalTable: "BowTieOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSessionResponseBowTieSelections_ExamSessionResponses_Ex~",
                        column: x => x.ExamSessionResponseId,
                        principalTable: "ExamSessionResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BowTieOptions_QuestionId",
                table: "BowTieOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionResponseBowTieSelections_BowTieOptionId",
                table: "ExamSessionResponseBowTieSelections",
                column: "BowTieOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSessionResponseBowTieSelections");

            migrationBuilder.DropTable(
                name: "BowTieOptions");
        }
    }
}
