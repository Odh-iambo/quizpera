using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScoresToExamSessionResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<decimal>(
        name: "MaximumPoints",
        table: "ExamSessionResponses",
        type: "numeric",
        nullable: false,
        defaultValue: 1m);

    migrationBuilder.AddColumn<decimal>(
        name: "PointsEarned",
        table: "ExamSessionResponses",
        type: "numeric",
        nullable: false,
        defaultValue: 0m);
}
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumPoints",
                table: "ExamSessionResponses");

            migrationBuilder.DropColumn(
                name: "PointsEarned",
                table: "ExamSessionResponses");
        }
    }
}
