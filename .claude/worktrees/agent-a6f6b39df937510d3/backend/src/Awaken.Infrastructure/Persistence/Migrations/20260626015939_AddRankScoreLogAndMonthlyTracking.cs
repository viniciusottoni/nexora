using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRankScoreLogAndMonthlyTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonthlyRankScoreGain",
                table: "hunter_progressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyRankScoreResetYearMonth",
                table: "hunter_progressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "rank_score_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RawGain = table.Column<int>(type: "integer", nullable: false),
                    Multiplier = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ExternalMultiplier = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    EffectiveGain = table.Column<int>(type: "integer", nullable: false),
                    WasMonthlyLimitApplied = table.Column<bool>(type: "boolean", nullable: false),
                    WasAbuseSuspected = table.Column<bool>(type: "boolean", nullable: false),
                    RankScoreAfter = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rank_score_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rank_score_logs_UserId",
                table: "rank_score_logs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rank_score_logs");

            migrationBuilder.DropColumn(
                name: "MonthlyRankScoreGain",
                table: "hunter_progressions");

            migrationBuilder.DropColumn(
                name: "MonthlyRankScoreResetYearMonth",
                table: "hunter_progressions");
        }
    }
}
