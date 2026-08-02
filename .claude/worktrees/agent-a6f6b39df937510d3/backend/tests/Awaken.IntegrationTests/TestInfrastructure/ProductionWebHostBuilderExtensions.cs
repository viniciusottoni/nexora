using Microsoft.AspNetCore.Hosting;

namespace Awaken.IntegrationTests;

internal static class ProductionWebHostBuilderExtensions
{
    private const string DefaultJwtSecret = "awaken-test-jwt-secret-that-is-long-enough-123456";
    private const string DefaultAdminJwtSecret = "awaken-test-admin-jwt-secret-that-is-long-enough-123456";
    private const string DefaultRedisConnectionString = "localhost:6380";
    private const string DefaultCorsOrigin = "https://app.awaken.test";

    public static IWebHostBuilder UseProductionTestDefaults(
        this IWebHostBuilder builder,
        string? corsOrigin = null)
    {
        builder.UseSetting("Jwt:Secret", DefaultJwtSecret);
        builder.UseSetting("AdminJwt:Secret", DefaultAdminJwtSecret);
        builder.UseSetting("ConnectionStrings:Redis", DefaultRedisConnectionString);
        builder.UseSetting("Cors:AllowedOrigins:0", corsOrigin ?? DefaultCorsOrigin);
        return builder;
    }
}
