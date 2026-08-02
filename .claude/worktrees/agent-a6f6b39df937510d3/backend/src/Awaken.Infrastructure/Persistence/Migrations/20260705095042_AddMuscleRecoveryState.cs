using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMuscleRecoveryState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "muscle_recovery_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MuscleGroup = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastTrainedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastIntensity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastMovementFamilies = table.Column<List<string>>(type: "text[]", nullable: false),
                    WeeklySetsAccumulated = table.Column<int>(type: "integer", nullable: false),
                    WeekAnchorDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_muscle_recovery_states", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_muscle_recovery_states_UserId_MuscleGroup",
                table: "muscle_recovery_states",
                columns: new[] { "UserId", "MuscleGroup" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "muscle_recovery_states");
        }
    }
}
