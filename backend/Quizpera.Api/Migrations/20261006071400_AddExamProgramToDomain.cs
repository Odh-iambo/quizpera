using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExamProgramToDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<Guid>(
        name: "ExamProgramId",
        table: "Domains",
        type: "uuid",
        nullable: true);

    migrationBuilder.Sql("""
        UPDATE "Domains"
        SET "ExamProgramId" = '77bbbd1d-4217-4e8d-adaf-2fe446ab9181'
        WHERE "Name" = 'Adult Health';
        """);

    migrationBuilder.AlterColumn<Guid>(
        name: "ExamProgramId",
        table: "Domains",
        type: "uuid",
        nullable: false,
        oldClrType: typeof(Guid),
        oldType: "uuid",
        oldNullable: true);

    migrationBuilder.CreateIndex(
        name: "IX_Domains_ExamProgramId",
        table: "Domains",
        column: "ExamProgramId");

    migrationBuilder.AddForeignKey(
        name: "FK_Domains_ExamPrograms_ExamProgramId",
        table: "Domains",
        column: "ExamProgramId",
        principalTable: "ExamPrograms",
        principalColumn: "Id",
        onDelete: ReferentialAction.Cascade);
}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Domains_ExamPrograms_ExamProgramId",
                table: "Domains");

            migrationBuilder.DropIndex(
                name: "IX_Domains_ExamProgramId",
                table: "Domains");

            migrationBuilder.DropColumn(
                name: "ExamProgramId",
                table: "Domains");
        }
    }
}
