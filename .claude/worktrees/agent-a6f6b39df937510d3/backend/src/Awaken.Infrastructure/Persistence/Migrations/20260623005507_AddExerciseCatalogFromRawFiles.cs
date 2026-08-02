using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseCatalogFromRawFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceFilePath",
                table: "exercise_raw_imports",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "exercise_catalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RawImportId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderExerciseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    NamePtBr = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NameOriginal = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DescriptionPtBr = table.Column<string>(type: "text", nullable: true),
                    InstructionsPtBr = table.Column<List<string>>(type: "text[]", nullable: false),
                    InstructionsOriginal = table.Column<List<string>>(type: "text[]", nullable: false),
                    TipsPtBr = table.Column<List<string>>(type: "text[]", nullable: false),
                    ExerciseType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MovementPattern = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MovementFamily = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Mechanic = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ForceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlaneOfMotion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Laterality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BodyPosition = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BenchAngle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EquipmentCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LoadType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PrimaryRegion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DifficultyLevel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DifficultyRank = table.Column<int>(type: "integer", nullable: false),
                    TechnicalComplexity = table.Column<int>(type: "integer", nullable: false),
                    ImpactLevel = table.Column<int>(type: "integer", nullable: false),
                    Environment = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequiredEquipment = table.Column<List<string>>(type: "text[]", nullable: false),
                    PrimaryMuscleGroups = table.Column<List<string>>(type: "text[]", nullable: false),
                    SecondaryMuscleGroups = table.Column<List<string>>(type: "text[]", nullable: false),
                    BodyParts = table.Column<List<string>>(type: "text[]", nullable: false),
                    JointStressTags = table.Column<List<string>>(type: "text[]", nullable: false),
                    ContraindicationTags = table.Column<List<string>>(type: "text[]", nullable: false),
                    LimitationBlockTags = table.Column<List<string>>(type: "text[]", nullable: false),
                    PainBlockTags = table.Column<List<string>>(type: "text[]", nullable: false),
                    GoalTags = table.Column<List<string>>(type: "text[]", nullable: false),
                    RiskTags = table.Column<List<string>>(type: "text[]", nullable: false),
                    AccessibilityTags = table.Column<List<string>>(type: "text[]", nullable: false),
                    TaxonomySignals = table.Column<List<string>>(type: "text[]", nullable: false),
                    MinExperienceLevel = table.Column<string>(type: "text", nullable: false),
                    SuitableForSedentary = table.Column<bool>(type: "boolean", nullable: false),
                    SuitableForBeginner = table.Column<bool>(type: "boolean", nullable: false),
                    SuitableForIntermediate = table.Column<bool>(type: "boolean", nullable: false),
                    SuitableForAdvanced = table.Column<bool>(type: "boolean", nullable: false),
                    IsCompound = table.Column<bool>(type: "boolean", nullable: false),
                    IsUnilateral = table.Column<bool>(type: "boolean", nullable: false),
                    IsAssisted = table.Column<bool>(type: "boolean", nullable: false),
                    IsWeighted = table.Column<bool>(type: "boolean", nullable: false),
                    RegressionExerciseIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    ProgressionExerciseIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    RelatedExerciseIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    VideoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    GifUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    MediaLicenseInfo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SanitizationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsApprovedForWorkoutGeneration = table.Column<bool>(type: "boolean", nullable: false),
                    Confidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_catalogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exercise_catalogs_exercise_raw_imports_RawImportId",
                        column: x => x.RawImportId,
                        principalTable: "exercise_raw_imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "exercise_attribute_contributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryAttribute = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StrengthXp = table.Column<int>(type: "integer", nullable: false),
                    AgilityXp = table.Column<int>(type: "integer", nullable: false),
                    EnduranceXp = table.Column<int>(type: "integer", nullable: false),
                    VitalityXp = table.Column<int>(type: "integer", nullable: false),
                    FocusXp = table.Column<int>(type: "integer", nullable: false),
                    WisdomXp = table.Column<int>(type: "integer", nullable: false),
                    IsAutoGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_attribute_contributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exercise_attribute_contributions_exercise_catalogs_Exercise~",
                        column: x => x.ExerciseCatalogId,
                        principalTable: "exercise_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_catalog_relations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelatedProviderExerciseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RelatedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RelationKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Types = table.Column<List<string>>(type: "text[]", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Confidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reasons = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_catalog_relations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exercise_catalog_relations_exercise_catalogs_ExerciseCatalo~",
                        column: x => x.ExerciseCatalogId,
                        principalTable: "exercise_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_attribute_contributions_ExerciseCatalogId",
                table: "exercise_attribute_contributions",
                column: "ExerciseCatalogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercise_catalog_relations_ExerciseCatalogId_RelationKind",
                table: "exercise_catalog_relations",
                columns: new[] { "ExerciseCatalogId", "RelationKind" });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_catalog_relations_RelatedProviderExerciseId",
                table: "exercise_catalog_relations",
                column: "RelatedProviderExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_catalogs_IsApprovedForWorkoutGeneration",
                table: "exercise_catalogs",
                column: "IsApprovedForWorkoutGeneration");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_catalogs_ProviderName_ProviderExerciseId",
                table: "exercise_catalogs",
                columns: new[] { "ProviderName", "ProviderExerciseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercise_catalogs_RawImportId",
                table: "exercise_catalogs",
                column: "RawImportId");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_catalogs_Slug",
                table: "exercise_catalogs",
                column: "Slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_attribute_contributions");

            migrationBuilder.DropTable(
                name: "exercise_catalog_relations");

            migrationBuilder.DropTable(
                name: "exercise_catalogs");

            migrationBuilder.DropColumn(
                name: "SourceFilePath",
                table: "exercise_raw_imports");
        }
    }
}
