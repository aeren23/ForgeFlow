using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeFlow.Work.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCiCdFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CiCdRunUrl",
                table: "Issues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CiCdStatus",
                table: "Issues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CiCdUpdatedAtUtc",
                table: "Issues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CiCdWorkflowName",
                table: "Issues",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CiCdRunUrl",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "CiCdStatus",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "CiCdUpdatedAtUtc",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "CiCdWorkflowName",
                table: "Issues");
        }
    }
}
