using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ContainerCpuPercent",
                table: "monitored_services",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerHealth",
                table: "monitored_services",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContainerId",
                table: "monitored_services",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerImage",
                table: "monitored_services",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ContainerMemoryBytes",
                table: "monitored_services",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerName",
                table: "monitored_services",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ContainerObservedAt",
                table: "monitored_services",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ContainerRestartCount",
                table: "monitored_services",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "ContainerRunning",
                table: "monitored_services",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ContainerStartedAt",
                table: "monitored_services",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainerCpuPercent",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "ContainerHealth",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "ContainerId",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "ContainerImage",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "ContainerMemoryBytes",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "ContainerName",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "ContainerObservedAt",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "ContainerRestartCount",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "ContainerRunning",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "ContainerStartedAt",
                table: "monitored_services");
        }
    }
}
