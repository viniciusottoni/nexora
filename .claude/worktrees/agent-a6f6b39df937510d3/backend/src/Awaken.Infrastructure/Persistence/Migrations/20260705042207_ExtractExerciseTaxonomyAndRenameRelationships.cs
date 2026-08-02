using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// US-236 — extrai a taxonomia biomecânica de <c>exercise_catalogs</c> para <c>exercise_taxonomies</c>
    /// (1:1, RN-003), preservando o dado real via backfill em SQL antes de derrubar as 16 colunas antigas
    /// (isso é o que elimina a duplicação de armazenamento). Também renomeia
    /// <c>exercise_catalog_relations</c> → <c>exercise_relationships</c> via <c>RenameTable</c> (não
    /// <c>DropTable</c>+<c>CreateTable</c> — preserva os candidatos de relação já importados), somando a
    /// coluna nova <c>DatasetVersion</c>, e adiciona <c>DatasetVersion</c> rastreável em
    /// <c>exercise_raw_imports</c>.
    ///
    /// Nota deliberada: o rename de tabela não renomeia os nomes físicos das constraints PK/FK herdadas
    /// (ficam como <c>PK_exercise_catalog_relations</c>/<c>FK_exercise_catalog_relations_...</c>). Isso é
    /// cosmético — a constraint continua funcionando normalmente sobre a tabela renomeada — e evita
    /// SQL adicional arriscado só por causa do nome.
    /// </summary>
    public partial class ExtractExerciseTaxonomyAndRenameRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Cria a tabela nova de taxonomia (ainda vazia).
            migrationBuilder.CreateTable(
                name: "exercise_taxonomies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementFamily = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MovementPattern = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Mechanic = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ForceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlaneOfMotion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Laterality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BodyPosition = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BenchAngle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EquipmentCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LoadType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PrimaryRegion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsCompound = table.Column<bool>(type: "boolean", nullable: false),
                    IsUnilateral = table.Column<bool>(type: "boolean", nullable: false),
                    IsAssisted = table.Column<bool>(type: "boolean", nullable: false),
                    IsWeighted = table.Column<bool>(type: "boolean", nullable: false),
                    Signals = table.Column<List<string>>(type: "text[]", nullable: false),
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
                    table.PrimaryKey("PK_exercise_taxonomies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exercise_taxonomies_exercise_catalogs_ExerciseCatalogId",
                        column: x => x.ExerciseCatalogId,
                        principalTable: "exercise_catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_taxonomies_ExerciseCatalogId",
                table: "exercise_taxonomies",
                column: "ExerciseCatalogId",
                unique: true);

            // 2) Backfill: copia o dado real de exercise_catalogs para exercise_taxonomies ANTES de
            // dropar as colunas antigas — nenhum dado é perdido nessa extração.
            migrationBuilder.Sql(
                """
                INSERT INTO exercise_taxonomies (
                    "Id", "ExerciseCatalogId", "MovementFamily", "MovementPattern", "Mechanic", "ForceType",
                    "PlaneOfMotion", "Laterality", "BodyPosition", "BenchAngle", "EquipmentCategory", "LoadType",
                    "PrimaryRegion", "IsCompound", "IsUnilateral", "IsAssisted", "IsWeighted", "Signals",
                    "Confidence", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted"
                )
                SELECT
                    gen_random_uuid(), "Id", "MovementFamily", "MovementPattern", "Mechanic", "ForceType",
                    "PlaneOfMotion", "Laterality", "BodyPosition", "BenchAngle", "EquipmentCategory", "LoadType",
                    "PrimaryRegion", "IsCompound", "IsUnilateral", "IsAssisted", "IsWeighted", "TaxonomySignals",
                    "Confidence", "CreatedAtUtc", "UpdatedAtUtc", false
                FROM exercise_catalogs;
                """);

            // 3) Dropa as 16 colunas antigas em exercise_catalogs — o dado agora mora só em exercise_taxonomies.
            migrationBuilder.DropColumn(name: "BenchAngle", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "BodyPosition", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "Confidence", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "EquipmentCategory", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "ForceType", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "IsAssisted", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "IsCompound", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "IsUnilateral", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "IsWeighted", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "Laterality", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "LoadType", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "Mechanic", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "MovementFamily", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "MovementPattern", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "PlaneOfMotion", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "PrimaryRegion", table: "exercise_catalogs");
            migrationBuilder.DropColumn(name: "TaxonomySignals", table: "exercise_catalogs");

            // 4) Rename (nao CreateTable+DropTable) — preserva os candidatos de relacao ja importados.
            migrationBuilder.RenameTable(
                name: "exercise_catalog_relations",
                newName: "exercise_relationships");

            migrationBuilder.RenameIndex(
                name: "IX_exercise_catalog_relations_ExerciseCatalogId_RelationKind",
                newName: "IX_exercise_relationships_ExerciseCatalogId_RelationKind",
                table: "exercise_relationships");

            migrationBuilder.RenameIndex(
                name: "IX_exercise_catalog_relations_RelatedProviderExerciseId",
                newName: "IX_exercise_relationships_RelatedProviderExerciseId",
                table: "exercise_relationships");

            migrationBuilder.AddColumn<string>(
                name: "DatasetVersion",
                table: "exercise_relationships",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            // 5) Rastreabilidade do dataset enriquecido (RN-001) no import.
            migrationBuilder.AddColumn<string>(
                name: "DatasetVersion",
                table: "exercise_raw_imports",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatasetVersion",
                table: "exercise_raw_imports");

            migrationBuilder.DropColumn(
                name: "DatasetVersion",
                table: "exercise_relationships");

            migrationBuilder.RenameIndex(
                name: "IX_exercise_relationships_RelatedProviderExerciseId",
                newName: "IX_exercise_catalog_relations_RelatedProviderExerciseId",
                table: "exercise_relationships");

            migrationBuilder.RenameIndex(
                name: "IX_exercise_relationships_ExerciseCatalogId_RelationKind",
                newName: "IX_exercise_catalog_relations_ExerciseCatalogId_RelationKind",
                table: "exercise_relationships");

            migrationBuilder.RenameTable(
                name: "exercise_relationships",
                newName: "exercise_catalog_relations");

            migrationBuilder.AddColumn<string>(
                name: "BenchAngle",
                table: "exercise_catalogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyPosition",
                table: "exercise_catalogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Confidence",
                table: "exercise_catalogs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EquipmentCategory",
                table: "exercise_catalogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ForceType",
                table: "exercise_catalogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAssisted",
                table: "exercise_catalogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompound",
                table: "exercise_catalogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnilateral",
                table: "exercise_catalogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWeighted",
                table: "exercise_catalogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Laterality",
                table: "exercise_catalogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LoadType",
                table: "exercise_catalogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Mechanic",
                table: "exercise_catalogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MovementFamily",
                table: "exercise_catalogs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MovementPattern",
                table: "exercise_catalogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlaneOfMotion",
                table: "exercise_catalogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryRegion",
                table: "exercise_catalogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<List<string>>(
                name: "TaxonomySignals",
                table: "exercise_catalogs",
                type: "text[]",
                nullable: false);

            // Copia o dado de volta de exercise_taxonomies antes de dropar a tabela.
            migrationBuilder.Sql(
                """
                UPDATE exercise_catalogs ec
                SET "MovementFamily" = et."MovementFamily",
                    "MovementPattern" = et."MovementPattern",
                    "Mechanic" = et."Mechanic",
                    "ForceType" = et."ForceType",
                    "PlaneOfMotion" = et."PlaneOfMotion",
                    "Laterality" = et."Laterality",
                    "BodyPosition" = et."BodyPosition",
                    "BenchAngle" = et."BenchAngle",
                    "EquipmentCategory" = et."EquipmentCategory",
                    "LoadType" = et."LoadType",
                    "PrimaryRegion" = et."PrimaryRegion",
                    "IsCompound" = et."IsCompound",
                    "IsUnilateral" = et."IsUnilateral",
                    "IsAssisted" = et."IsAssisted",
                    "IsWeighted" = et."IsWeighted",
                    "TaxonomySignals" = et."Signals",
                    "Confidence" = et."Confidence"
                FROM exercise_taxonomies et
                WHERE et."ExerciseCatalogId" = ec."Id";
                """);

            migrationBuilder.DropTable(
                name: "exercise_taxonomies");
        }
    }
}
