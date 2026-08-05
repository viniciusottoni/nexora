using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashSessionUniqueOpenIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_cash_closed",
                table: "cash_session",
                sql: "status <> 2 OR (closed_at IS NOT NULL AND counted_amount IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cash_opening",
                table: "cash_session",
                sql: "opening_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_movement_amount",
                table: "cash_movement",
                sql: "amount > 0");

            migrationBuilder.CreateIndex(
                name: "uq_cash_open",
                table: "cash_session",
                columns: new[] { "store_id", "operator_id" },
                unique: true,
                filter: "status <> 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_cash_open",
                table: "cash_session");

            migrationBuilder.DropCheckConstraint(
                name: "ck_movement_amount",
                table: "cash_movement");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cash_opening",
                table: "cash_session");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cash_closed",
                table: "cash_session");
        }
    }
}
