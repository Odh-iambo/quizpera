using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizpera.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalCaseCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.AddColumn<Guid>(
                 name: "DomainId",
                table: "ClinicalCases",
                 type: "uuid",
                  nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                table: "ClinicalCases",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCases_DomainId",
                table: "ClinicalCases",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalCases_SubjectId",
                table: "ClinicalCases",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalCases_Domains_DomainId",
                table: "ClinicalCases",
                column: "DomainId",
                principalTable: "Domains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalCases_Subjects_SubjectId",
                table: "ClinicalCases",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalCases_Domains_DomainId",
                table: "ClinicalCases");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalCases_Subjects_SubjectId",
                table: "ClinicalCases");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalCases_DomainId",
                table: "ClinicalCases");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalCases_SubjectId",
                table: "ClinicalCases");

            migrationBuilder.DropColumn(
                name: "DomainId",
                table: "ClinicalCases");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "ClinicalCases");
        }
    }
}
