using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260620000001_AddPhysicalLimitationsAndPainsToUserProfile")]
    public partial class AddPhysicalLimitationsAndPainsToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "PhysicalLimitations",
                table: "user_profiles",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "PhysicalPains",
                table: "user_profiles",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhysicalLimitations",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "PhysicalPains",
                table: "user_profiles");
        }
    }
}
