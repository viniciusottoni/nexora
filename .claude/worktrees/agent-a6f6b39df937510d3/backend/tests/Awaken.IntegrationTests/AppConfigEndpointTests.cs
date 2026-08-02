using System.Net;
using System.Net.Http.Json;
using Awaken.Contracts.AppConfig;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Awaken.IntegrationTests;

public class AppConfigEndpointTests
{
    private readonly HttpClient _client;

    public AppConfigEndpointTests()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Development"));
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    [Fact]
    public async Task GetPlansReturns200WithTrialDays7AndTwoPlans()
    {
        var response = await _client.GetAsync("/api/app-config/plans?platform=android&locale=pt-BR");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PlansConfigResponse>();
        body!.TrialDays.Should().Be(7);
        body.Plans.Should().HaveCount(2);
        body.Plans[0].Id.Should().Be("monthly");
        body.Plans[0].Label.Should().Be("Mensal");
        body.Plans[0].PriceLabel.Should().Be("R$ 14,90");
        body.Plans[1].Id.Should().Be("annual");
        body.Plans[1].Label.Should().Be("Anual");
        body.Plans[1].Highlighted.Should().BeTrue();
    }

    [Theory]
    [InlineData("en", "Monthly", "R$ 14.90", "/month", "Annual", "R$ 99.90", "/year")]
    [InlineData("es", "Mensual", "R$ 14,90", "/mes", "Anual", "R$ 99,90", "/año")]
    [InlineData("fr", "Mensuel", "R$ 14,90", "/mois", "Annuel", "R$ 99,90", "/an")]
    public async Task GetPlansReturnsLocalizedLabelsByLocale(
        string locale,
        string monthlyLabel,
        string monthlyPrice,
        string monthlyPeriod,
        string annualLabel,
        string annualPrice,
        string annualPeriod)
    {
        var response = await _client.GetAsync($"/api/app-config/plans?platform=android&locale={locale}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PlansConfigResponse>();
        body!.Plans[0].Label.Should().Be(monthlyLabel);
        body.Plans[0].PriceLabel.Should().Be(monthlyPrice);
        body.Plans[0].PeriodLabel.Should().Be(monthlyPeriod);
        body.Plans[1].Label.Should().Be(annualLabel);
        body.Plans[1].PriceLabel.Should().Be(annualPrice);
        body.Plans[1].PeriodLabel.Should().Be(annualPeriod);
    }

    [Fact]
    public async Task GetPlansDoesNotRequireAuthentication()
    {
        var response = await _client.GetAsync("/api/app-config/plans?platform=android&locale=pt-BR");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
