using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeFlow.Artifact.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrelationIdToRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "ArtifactRevisions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactRevisions_CorrelationId",
                table: "ArtifactRevisions",
                column: "CorrelationId",
                unique: true,
                filter: "[CorrelationId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArtifactRevisions_CorrelationId",
                table: "ArtifactRevisions");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "ArtifactRevisions");
        }
    }
}
