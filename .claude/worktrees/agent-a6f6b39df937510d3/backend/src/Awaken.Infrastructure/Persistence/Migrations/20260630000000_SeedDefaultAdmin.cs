using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultAdmin : Migration
    {
        private const string AdminId = "47e91c55-1d41-4d6d-b858-5c3a526aa44f";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                INSERT INTO admin_users (
                    "Id", "Email", "PasswordHash", "FailedLoginAttempts",
                    "LockedUntilUtc", "MfaSecretEncrypted", "MfaEnabled",
                    "Status", "LastLoginAtUtc",
                    "CreatedAtUtc", "UpdatedAtUtc", "DeletedAtUtc",
                    "CreatedByUserId", "UpdatedByUserId", "IsDeleted"
                ) VALUES (
                    '{AdminId}',
                    'vin.ottoni@gmail.com',
                    '$2a$12$dlrRb.Eb7a1Ok26n4gKSOeqmsN28OTuB7igWOYErfB/UC17N2x11m',
                    0,
                    NULL, NULL, false,
                    'active', NULL,
                    NOW() AT TIME ZONE 'UTC', NULL, NULL,
                    NULL, NULL, false
                )
                ON CONFLICT ("Email") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM admin_users WHERE "Id" = '{AdminId}';
                """);
        }
    }
}
