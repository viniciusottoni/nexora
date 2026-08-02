using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemRepeatedFromColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "repeated_from_item_id",
                table: "order_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_item_repeated_from_item_id",
                table: "order_item",
                column: "repeated_from_item_id");

            migrationBuilder.AddForeignKey(
                name: "fk_order_item_order_item_repeated_from_item_id",
                table: "order_item",
                column: "repeated_from_item_id",
                principalTable: "order_item",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_item_order_item_repeated_from_item_id",
                table: "order_item");

            migrationBuilder.DropIndex(
                name: "ix_order_item_repeated_from_item_id",
                table: "order_item");

            migrationBuilder.DropColumn(
                name: "repeated_from_item_id",
                table: "order_item");
        }
    }
}
