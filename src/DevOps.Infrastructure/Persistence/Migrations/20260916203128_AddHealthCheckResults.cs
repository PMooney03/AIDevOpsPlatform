using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthCheckResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailureCount",
                table: "monitored_services",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UnreachableSince",
                table: "monitored_services",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "health_check_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_check_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_health_check_results_monitored_services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "monitored_services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_health_check_results_ServiceId_CheckedAt",
                table: "health_check_results",
                columns: new[] { "ServiceId", "CheckedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "health_check_results");

            migrationBuilder.DropColumn(
                name: "ConsecutiveFailureCount",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "UnreachableSince",
                table: "monitored_services");
        }
    }
}
