using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeFlow.AiOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PromptTokens = table.Column<int>(type: "int", nullable: false),
                    CompletionTokens = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAuditLogs_CorrelationId",
                table: "AiAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAuditLogs_CreatedAtUtc",
                table: "AiAuditLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiAuditLogs_IssueKey",
                table: "AiAuditLogs",
                column: "IssueKey");

            migrationBuilder.CreateIndex(
                name: "IX_AiAuditLogs_RequestId",
                table: "AiAuditLogs",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAuditLogs");
        }
    }
}
