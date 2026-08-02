using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingProgramSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "training_program_splits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SplitMapVersion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DayCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_program_splits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "training_split_days",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingProgramSplitId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayIndex = table.Column<int>(type: "integer", nullable: false),
                    DayKey = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    LabelI18nKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetMuscleGroups = table.Column<List<string>>(type: "text[]", nullable: false),
                    SecondaryMuscleGroups = table.Column<List<string>>(type: "text[]", nullable: false),
                    TargetMovementPatterns = table.Column<List<string>>(type: "text[]", nullable: false),
                    AllowsCoreFinisher = table.Column<bool>(type: "boolean", nullable: false),
                    MinExercises = table.Column<int>(type: "integer", nullable: false),
                    MaxExercises = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_split_days", x => x.Id);
                    table.ForeignKey(
                        name: "FK_training_split_days_training_program_splits_TrainingProgram~",
                        column: x => x.TrainingProgramSplitId,
                        principalTable: "training_program_splits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_training_program_splits_ProgramKey",
                table: "training_program_splits",
                column: "ProgramKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_split_days_TrainingProgramSplitId_DayIndex",
                table: "training_split_days",
                columns: new[] { "TrainingProgramSplitId", "DayIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "training_split_days");

            migrationBuilder.DropTable(
                name: "training_program_splits");
        }
    }
}
