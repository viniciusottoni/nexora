using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNutritionPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_nutrition_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CupVolumeMl = table.Column<int>(type: "integer", nullable: false, defaultValue: 250),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_nutrition_preferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_nutrition_preferences_UserId",
                table: "user_nutrition_preferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_nutrition_preferences");
        }
    }
}
