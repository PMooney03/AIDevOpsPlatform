using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiAnalysisAndDeployments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActionsTaken",
                table: "incidents",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCause",
                table: "incidents",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_analyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ProbableCause = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SeverityAssessment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EvidenceJson = table.Column<string>(type: "text", nullable: false),
                    RecommendedChecksJson = table.Column<string>(type: "text", nullable: false),
                    SuggestedRemediationJson = table.Column<string>(type: "text", nullable: false),
                    LimitationsJson = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_analyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_analyses_incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitSha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Branch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BuildStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TestStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DeploymentStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deployments_monitored_services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "monitored_services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_analyses_IncidentId_CreatedAt",
                table: "ai_analyses",
                columns: new[] { "IncidentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_deployments_ServiceId_StartedAt",
                table: "deployments",
                columns: new[] { "ServiceId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_analyses");

            migrationBuilder.DropTable(
                name: "deployments");

            migrationBuilder.DropColumn(
                name: "ActionsTaken",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "RootCause",
                table: "incidents");
        }
    }
}
