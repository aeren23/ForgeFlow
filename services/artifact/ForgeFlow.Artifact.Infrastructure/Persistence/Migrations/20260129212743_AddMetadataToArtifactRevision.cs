using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeFlow.Artifact.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataToArtifactRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "ArtifactRevisions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "ArtifactRevisions");
        }
    }
}
