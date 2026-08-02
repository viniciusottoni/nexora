using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetExerciseCatalogToRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetExerciseCatalogId",
                table: "exercise_relationships",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercise_relationships_TargetExerciseCatalogId",
                table: "exercise_relationships",
                column: "TargetExerciseCatalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_exercise_relationships_exercise_catalogs_TargetExerciseCata~",
                table: "exercise_relationships",
                column: "TargetExerciseCatalogId",
                principalTable: "exercise_catalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exercise_relationships_exercise_catalogs_TargetExerciseCata~",
                table: "exercise_relationships");

            migrationBuilder.DropIndex(
                name: "IX_exercise_relationships_TargetExerciseCatalogId",
                table: "exercise_relationships");

            migrationBuilder.DropColumn(
                name: "TargetExerciseCatalogId",
                table: "exercise_relationships");
        }
    }
}
