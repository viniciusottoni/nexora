using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;

namespace Awaken.IntegrationTests;

public class ApiSmokeTests
{
    [Fact]
    public async Task SwaggerJson_IsServed_WhenApiBootsInDevelopment()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("info").GetProperty("title")
            .GetString().Should().Be("Awaken API");
        document.RootElement.GetProperty("info").GetProperty("version")
            .GetString().Should().Be("v1");
    }
}
