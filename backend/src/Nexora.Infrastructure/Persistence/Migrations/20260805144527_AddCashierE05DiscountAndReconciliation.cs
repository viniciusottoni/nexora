using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashierE05DiscountAndReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "discount_applied_by",
                table: "table_session",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "discount_authorized_by",
                table: "table_session",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_percent",
                table: "table_session",
                type: "money_amount",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "discount_reason",
                table: "table_session",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_scope",
                table: "table_session",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "service_fee_waive_reason",
                table: "table_session",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "service_fee_waive_scope",
                table: "table_session",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "service_fee_waived",
                table: "table_session",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "service_fee_waived_by",
                table: "table_session",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reconciliation_status",
                table: "payment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "discount",
                table: "order_item",
                type: "money_amount",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "discount_applied_by",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "discount_authorized_by",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_reason",
                table: "order_item",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "discount_applied_by",
                table: "table_session");

            migrationBuilder.DropColumn(
                name: "discount_authorized_by",
                table: "table_session");

            migrationBuilder.DropColumn(
                name: "discount_percent",
                table: "table_session");

            migrationBuilder.DropColumn(
                name: "discount_reason",
                table: "table_session");

            migrationBuilder.DropColumn(
                name: "discount_scope",
                table: "table_session");

            migrationBuilder.DropColumn(
                name: "service_fee_waive_reason",
                table: "table_session");

            migrationBuilder.DropColumn(
                name: "service_fee_waive_scope",
                table: "table_session");

            migrationBuilder.DropColumn(
                name: "service_fee_waived",
                table: "table_session");

            migrationBuilder.DropColumn(
                name: "service_fee_waived_by",
                table: "table_session");

            migrationBuilder.DropColumn(
                name: "reconciliation_status",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "discount",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "discount_applied_by",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "discount_authorized_by",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "discount_reason",
                table: "order_item");
        }
    }
}
