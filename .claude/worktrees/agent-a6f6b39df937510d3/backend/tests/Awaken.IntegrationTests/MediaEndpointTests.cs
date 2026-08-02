using Awaken.Application.Common.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Awaken.IntegrationTests;

public class MediaEndpointTests
{
    [Fact]
    public async Task GetImageRedirectsToPresignedUrl()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IMediaRedirectService, FakeMediaRedirectService>();
                });
            });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/api/media/images/gold.png");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be("https://signed.example/images/gold.png");
    }

    [Fact]
    public async Task GetExerciseRedirectsToPresignedUrl()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IMediaRedirectService, FakeMediaRedirectService>();
                });
            });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/api/media/exercises/0025/360.gif");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be("https://signed.example/exercises/0025/360.gif");
    }
}

internal sealed class FakeMediaRedirectService : IMediaRedirectService
{
    public string CreatePresignedReadUrl(string key) => $"https://signed.example/{key}";
}
