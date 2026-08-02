using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPinUniquenessIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_app_user_pin",
                table: "app_user");

            migrationBuilder.CreateIndex(
                name: "uq_app_user_pin",
                table: "app_user",
                columns: new[] { "tenant_id", "pin_lookup" },
                unique: true,
                filter: "pin_lookup IS NOT NULL AND status = 0 AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_app_user_pin",
                table: "app_user");

            migrationBuilder.CreateIndex(
                name: "uq_app_user_pin",
                table: "app_user",
                columns: new[] { "tenant_id", "pin_hash" },
                unique: true,
                filter: "pin_hash IS NOT NULL AND status = 0 AND deleted_at IS NULL");
        }
    }
}
