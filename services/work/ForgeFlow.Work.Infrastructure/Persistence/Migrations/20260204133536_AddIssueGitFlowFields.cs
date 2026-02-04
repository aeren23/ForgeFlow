using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeFlow.Work.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueGitFlowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BranchName",
                table: "Issues",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "Issues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_BranchName",
                table: "Issues",
                column: "BranchName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Issues_BranchName",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "BranchName",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "Issues");
        }
    }
}
