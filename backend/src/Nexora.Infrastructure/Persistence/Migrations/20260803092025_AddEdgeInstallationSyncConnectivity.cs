using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeInstallationSyncConnectivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "connectivity",
                table: "edge_installation",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "offline_since",
                table: "edge_installation",
                type: "timestamptz",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "connectivity",
                table: "edge_installation");

            migrationBuilder.DropColumn(
                name: "offline_since",
                table: "edge_installation");
        }
    }
}
