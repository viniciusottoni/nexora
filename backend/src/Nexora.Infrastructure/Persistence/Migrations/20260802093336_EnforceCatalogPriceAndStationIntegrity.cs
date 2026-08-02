using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceCatalogPriceAndStationIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "uq_station_current_bottleneck_tenant_store",
                table: "station",
                columns: new[] { "tenant_id", "store_id" },
                unique: true,
                filter: "deleted_at IS NULL AND is_bottleneck = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_product_station_id",
                table: "product",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "uq_price_current_tenant_variant_channel",
                table: "price",
                columns: new[] { "tenant_id", "variant_id", "channel" },
                unique: true,
                filter: "valid_to IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_amount_non_negative",
                table: "price",
                sql: "amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_valid_period",
                table: "price",
                sql: "valid_to IS NULL OR valid_to > valid_from");

            migrationBuilder.AddForeignKey(
                name: "fk_product_stations_station_id",
                table: "product",
                column: "station_id",
                principalTable: "station",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_stations_station_id",
                table: "product");

            migrationBuilder.DropIndex(
                name: "uq_station_current_bottleneck_tenant_store",
                table: "station");

            migrationBuilder.DropIndex(
                name: "ix_product_station_id",
                table: "product");

            migrationBuilder.DropIndex(
                name: "uq_price_current_tenant_variant_channel",
                table: "price");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_amount_non_negative",
                table: "price");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_valid_period",
                table: "price");
        }
    }
}
