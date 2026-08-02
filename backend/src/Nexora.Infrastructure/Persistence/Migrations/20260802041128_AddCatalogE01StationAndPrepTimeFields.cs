using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogE01StationAndPrepTimeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "station",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_bottleneck",
                table: "station",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "critical_minutes",
                table: "product_variant",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "warn_minutes",
                table: "product_variant",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "color",
                table: "station");

            migrationBuilder.DropColumn(
                name: "is_bottleneck",
                table: "station");

            migrationBuilder.DropColumn(
                name: "critical_minutes",
                table: "product_variant");

            migrationBuilder.DropColumn(
                name: "warn_minutes",
                table: "product_variant");
        }
    }
}
