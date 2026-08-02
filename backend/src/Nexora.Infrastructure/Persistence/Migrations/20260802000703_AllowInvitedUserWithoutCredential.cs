using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowInvitedUserWithoutCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_app_user_credential",
                table: "app_user");

            migrationBuilder.AddCheckConstraint(
                name: "ck_app_user_credential",
                table: "app_user",
                sql: "password_hash IS NOT NULL OR pin_hash IS NOT NULL OR status = 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_app_user_credential",
                table: "app_user");

            migrationBuilder.AddCheckConstraint(
                name: "ck_app_user_credential",
                table: "app_user",
                sql: "password_hash IS NOT NULL OR pin_hash IS NOT NULL");
        }
    }
}
