using Awaken.Api.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Awaken.UnitTests.Configuration;

public class StartupConfigurationValidatorTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static IWebHostEnvironment MakeEnv(string environmentName)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(environmentName);
        return env.Object;
    }

    private static Dictionary<string, string?> ValidProductionValues() => new()
    {
        ["Jwt:Secret"] = "a-very-strong-jwt-secret-that-is-long-enough",
        ["AdminJwt:Secret"] = "a-very-strong-admin-jwt-secret-long-enough",
        ["Cors:AllowedOrigins:0"] = "https://app.awaken.com",
        ["ConnectionStrings:PostgreSQL"] = "Host=prod-db;Database=awaken;",
        ["ConnectionStrings:Redis"] = "prod-redis:6379"
    };

    [Fact]
    public void WhenDevelopment_DoesNotThrowEvenWithEmptyConfig()
    {
        var config = BuildConfig([]);
        var env = MakeEnv("Development");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().NotThrow();
    }

    [Fact]
    public void WhenProduction_WithValidConfig_DoesNotThrow()
    {
        var config = BuildConfig(ValidProductionValues());
        var env = MakeEnv("Production");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().NotThrow();
    }

    [Fact]
    public void WhenProduction_WithEmptyJwtSecret_Throws()
    {
        var values = ValidProductionValues();
        values["Jwt:Secret"] = "";
        var config = BuildConfig(values);
        var env = MakeEnv("Production");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public void WhenProduction_WithShortJwtSecret_Throws()
    {
        var values = ValidProductionValues();
        values["Jwt:Secret"] = "short";
        var config = BuildConfig(values);
        var env = MakeEnv("Production");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public void WhenProduction_WithPlaceholderJwtSecret_Throws()
    {
        var values = ValidProductionValues();
        values["Jwt:Secret"] = "CHANGE_THIS_to_your_real_jwt_secret_value_here";
        var config = BuildConfig(values);
        var env = MakeEnv("Production");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public void WhenStaging_WithEmptyJwtSecret_Throws()
    {
        var values = ValidProductionValues();
        values["Jwt:Secret"] = "";
        var config = BuildConfig(values);
        var env = MakeEnv("Staging");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public void WhenProduction_WithoutCorsOrigins_Throws()
    {
        var values = ValidProductionValues();
        values.Remove("Cors:AllowedOrigins:0");
        var config = BuildConfig(values);
        var env = MakeEnv("Production");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cors:AllowedOrigins*");
    }

    [Fact]
    public void WhenProduction_WithPlaceholderGoogleClientId_Throws()
    {
        var values = ValidProductionValues();
        values["Google:ClientId"] = "your-google-client-id.apps.googleusercontent.com";
        var config = BuildConfig(values);
        var env = MakeEnv("Production");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Google:ClientId*");
    }

    [Fact]
    public void WhenProduction_WithEmptyGoogleClientId_DoesNotThrowForGoogleSection()
    {
        var values = ValidProductionValues();
        // Not setting Google:ClientId means it's absent — disabled, which is fine
        var config = BuildConfig(values);
        var env = MakeEnv("Production");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().NotThrow();
    }

    [Fact]
    public void WhenProduction_WithMissingPostgresConnection_Throws()
    {
        var values = ValidProductionValues();
        values["ConnectionStrings:PostgreSQL"] = null;
        var config = BuildConfig(values);
        var env = MakeEnv("Production");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PostgreSQL*");
    }

    [Fact]
    public void WhenProduction_WithMissingRedisConnection_Throws()
    {
        var values = ValidProductionValues();
        values["ConnectionStrings:Redis"] = null;
        var config = BuildConfig(values);
        var env = MakeEnv("Production");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Redis*");
    }

    [Fact]
    public void WhenProduction_MultipleErrors_AllReportedInSingleException()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.awaken.com",
            ["ConnectionStrings:PostgreSQL"] = "Host=prod-db;",
            ["ConnectionStrings:Redis"] = "prod-redis:6379"
            // Jwt:Secret and AdminJwt:Secret missing
        });
        var env = MakeEnv("Production");

        var act = () => StartupConfigurationValidator.ValidateCriticalConfiguration(config, env);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*[1]*")
            .And.Message.Should().Contain("[2]");
    }
}
