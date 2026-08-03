using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemTransitionAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "fired_device_id",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "oven_in_by",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "oven_in_device_id",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "oven_out_by",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "oven_out_device_id",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "placed_device_id",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ready_device_id",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "served_device_id",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_item_sequence",
                table: "order_item",
                sql: "(fired_at IS NULL OR fired_at >= placed_at)\nAND (oven_in_at IS NULL OR (fired_at IS NOT NULL AND oven_in_at >= fired_at))\nAND (oven_out_at IS NULL OR (oven_in_at IS NOT NULL AND oven_out_at >= oven_in_at))\nAND (ready_at IS NULL OR (fired_at IS NOT NULL AND ready_at >= fired_at AND (oven_out_at IS NULL OR ready_at >= oven_out_at)))\nAND (served_at IS NULL OR (ready_at IS NOT NULL AND served_at >= ready_at))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_item_sequence",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "fired_device_id",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "oven_in_by",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "oven_in_device_id",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "oven_out_by",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "oven_out_device_id",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "placed_device_id",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "ready_device_id",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "served_device_id",
                table: "order_item");
        }
    }
}
