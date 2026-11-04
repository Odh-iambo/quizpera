using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMatrixGridSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionMatrixColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionMatrixColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionMatrixColumns_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionMatrixRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionMatrixRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionMatrixRows_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamSessionMatrixAnswer",
                columns: table => new
                {
                    ExamSessionResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatrixRowId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedColumnId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessionMatrixAnswer", x => new { x.ExamSessionResponseId, x.MatrixRowId });
                    table.ForeignKey(
                        name: "FK_ExamSessionMatrixAnswer_ExamSessionResponses_ExamSessionRes~",
                        column: x => x.ExamSessionResponseId,
                        principalTable: "ExamSessionResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSessionMatrixAnswer_QuestionMatrixColumns_SelectedColum~",
                        column: x => x.SelectedColumnId,
                        principalTable: "QuestionMatrixColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSessionMatrixAnswer_QuestionMatrixRows_MatrixRowId",
                        column: x => x.MatrixRowId,
                        principalTable: "QuestionMatrixRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionMatrixAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatrixRowId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectColumnId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionMatrixAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionMatrixAnswers_QuestionMatrixColumns_CorrectColumnId",
                        column: x => x.CorrectColumnId,
                        principalTable: "QuestionMatrixColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionMatrixAnswers_QuestionMatrixRows_MatrixRowId",
                        column: x => x.MatrixRowId,
                        principalTable: "QuestionMatrixRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionMatrixAnswer_MatrixRowId",
                table: "ExamSessionMatrixAnswer",
                column: "MatrixRowId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessionMatrixAnswer_SelectedColumnId",
                table: "ExamSessionMatrixAnswer",
                column: "SelectedColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionMatrixAnswers_CorrectColumnId",
                table: "QuestionMatrixAnswers",
                column: "CorrectColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionMatrixAnswers_MatrixRowId",
                table: "QuestionMatrixAnswers",
                column: "MatrixRowId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionMatrixColumns_QuestionId",
                table: "QuestionMatrixColumns",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionMatrixRows_QuestionId",
                table: "QuestionMatrixRows",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSessionMatrixAnswer");

            migrationBuilder.DropTable(
                name: "QuestionMatrixAnswers");

            migrationBuilder.DropTable(
                name: "QuestionMatrixColumns");

            migrationBuilder.DropTable(
                name: "QuestionMatrixRows");
        }
    }
}
