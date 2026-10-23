using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSessionOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ExamSessions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_UserId",
                table: "ExamSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSessions_AspNetUsers_UserId",
                table: "ExamSessions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamSessions_AspNetUsers_UserId",
                table: "ExamSessions");

            migrationBuilder.DropIndex(
                name: "IX_ExamSessions_UserId",
                table: "ExamSessions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ExamSessions");
        }
    }
}
