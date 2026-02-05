using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeFlow.GitHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialGitHubSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Installations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstallationId = table.Column<long>(type: "bigint", nullable: false),
                    AccountLogin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstalledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepositoryConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepositoryId = table.Column<long>(type: "bigint", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DefaultBranch = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "main"),
                    WebhookSecret = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InstallationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepositoryConnections_Installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Installations_InstallationId",
                table: "Installations",
                column: "InstallationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryConnections_InstallationId",
                table: "RepositoryConnections",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryConnections_ProjectId",
                table: "RepositoryConnections",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryConnections_RepositoryId",
                table: "RepositoryConnections",
                column: "RepositoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepositoryConnections");

            migrationBuilder.DropTable(
                name: "Installations");
        }
    }
}
