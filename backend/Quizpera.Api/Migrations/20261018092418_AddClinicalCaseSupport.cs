using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalCaseSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicalCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Introduction = table.Column<string>(type: "text", nullable: false),
                    ExamProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalCases_ExamPrograms_ExamProgramId",
                        column: x => x.ExamProgramId,
                        principalTable: "ExamPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalCaseStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicalCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ClinicalInformation = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalCaseStages", x => x.Id);
                    table.UniqueConstraint("AK_ClinicalCaseStages_ClinicalCaseId_Id", x => new { x.ClinicalCaseId, x.Id });
                    table.ForeignKey(
                        name: "FK_ClinicalCaseStages_ClinicalCases_ClinicalCaseId",
                        column: x => x.ClinicalCaseId,
                        principalTable: "ClinicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalCaseQuestions",
                columns: table => new
                {
                    ClinicalCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicalCaseStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    ClinicalJudgmentFunction = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalCaseQuestions", x => new { x.ClinicalCaseId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_ClinicalCaseQuestions_ClinicalCaseStages_ClinicalCaseId_Cli~",
                        columns: x => new { x.ClinicalCaseId, x.ClinicalCaseStageId },
                        principalTable: "ClinicalCaseStages",
                        principalColumns: new[] { "ClinicalCaseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalCaseQuestions_ClinicalCases_ClinicalCaseId",
                        column: x => x.ClinicalCaseId,
                        principalTable: "ClinicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClinicalCaseQuestions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamSessionCaseProgress",
                columns: table => new
                {
                    ExamSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicalCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessionCaseProgress", x => new { x.ExamSessionId, x.ClinicalCaseId });
                    table.ForeignKey(
                        name: "FK_ExamSessionCaseProgress_ClinicalCaseStages_ClinicalCaseId_C~",
                        columns: x => new { x.ClinicalCaseId, x.CurrentStageId },
                        principalTable: "ClinicalCaseStages",
                        principalColumns: new[] { "ClinicalCaseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSessionCaseProgress_ClinicalCases_ClinicalCaseId",
                        column: x => x.ClinicalCaseId,
                        principalTable: "ClinicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSessionCaseProgress_ExamSessions_ExamSessionId",
                        column: x => x.ExamSessionId,
                        principalTable: "ExamSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCaseQuestions_ClinicalCaseId_ClinicalCaseStageId",
                table: "ClinicalCaseQuestions",
                columns: new[] { "ClinicalCaseId", "ClinicalCaseStageId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCaseQuestions_ClinicalCaseId_DisplayOrder",
                table: "ClinicalCaseQuestions",
                columns: new[] { "ClinicalCaseId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCaseQuestions_QuestionId",
                table: "ClinicalCaseQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCases_ExamProgramId",
                table: "ClinicalCases",
                column: "ExamProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCaseStages_ClinicalCaseId_DisplayOrder",
                table: "ClinicalCaseStages",
                columns: new[] { "ClinicalCaseId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionCaseProgress_ClinicalCaseId_CurrentStageId",
                table: "ExamSessionCaseProgress",
                columns: new[] { "ClinicalCaseId", "CurrentStageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicalCaseQuestions");

            migrationBuilder.DropTable(
                name: "ExamSessionCaseProgress");

            migrationBuilder.DropTable(
                name: "ClinicalCaseStages");

            migrationBuilder.DropTable(
                name: "ClinicalCases");
        }
    }
}
