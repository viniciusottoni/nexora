using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseRawImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exercise_raw_imports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderExerciseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImportBatchId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    MediaBaseUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    MediaLicenseInfo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_raw_imports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_raw_imports_ImportBatchId",
                table: "exercise_raw_imports",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_raw_imports_ProviderName_ProviderExerciseId",
                table: "exercise_raw_imports",
                columns: new[] { "ProviderName", "ProviderExerciseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_raw_imports");
        }
    }
}
