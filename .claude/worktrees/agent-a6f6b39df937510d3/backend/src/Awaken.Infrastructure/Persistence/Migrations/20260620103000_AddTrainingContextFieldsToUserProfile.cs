using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260620103000_AddTrainingContextFieldsToUserProfile")]
    public partial class AddTrainingContextFieldsToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailableDaysPerWeek",
                table: "user_profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "EquipmentAvailable",
                table: "user_profiles",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrainingLocation",
                table: "user_profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "TrainingPreferences",
                table: "user_profiles",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableDaysPerWeek",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "EquipmentAvailable",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "TrainingLocation",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "TrainingPreferences",
                table: "user_profiles");
        }
    }
}
