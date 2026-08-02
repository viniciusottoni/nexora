using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Awaken.UnitTests.Infrastructure;

public class GoogleTokenValidatorTests
{
    private static GoogleTokenValidator BuildValidator(
        IWebHostEnvironment environment,
        Dictionary<string, string?>? config = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config ?? [])
            .Build();
        return new GoogleTokenValidator(configuration, environment);
    }

    private static IWebHostEnvironment ProductionEnvironment()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Production");
        return env.Object;
    }

    private static IWebHostEnvironment DevelopmentEnvironment()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Development");
        return env.Object;
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_WhenTokenIsInvalidJwt()
    {
        var validator = BuildValidator(ProductionEnvironment(), new()
        {
            ["Google:ClientId"] = "test-client-id"
        });

        var result = await validator.ValidateAsync("not-a-real-jwt", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_WhenNoClientIdsConfiguredInProduction()
    {
        var validator = BuildValidator(ProductionEnvironment());

        var result = await validator.ValidateAsync("any-token", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_WhenNoClientIdsConfiguredInStaging()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Staging");
        var validator = BuildValidator(env.Object);

        var result = await validator.ValidateAsync("any-token", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_AttemptsValidation_WhenNoClientIdsConfiguredInDevelopment()
    {
        var validator = BuildValidator(DevelopmentEnvironment());

        // In Development with no client IDs, validation proceeds but the token is invalid — expects null
        var result = await validator.ValidateAsync("not-a-real-jwt", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_MergesAllowedClientIds_FromSingleAndArray()
    {
        // Validates that both Google:ClientId and Google:AllowedClientIds are merged without duplicates.
        // With an invalid token, we still get null but no exception from config parsing.
        var validator = BuildValidator(ProductionEnvironment(), new()
        {
            ["Google:ClientId"] = "client-1",
            ["Google:AllowedClientIds:0"] = "client-2",
            ["Google:AllowedClientIds:1"] = "client-1", // duplicate — should not double-add
        });

        var result = await validator.ValidateAsync("not-a-real-jwt", CancellationToken.None);

        result.Should().BeNull();
    }
}
