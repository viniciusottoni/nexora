using Awaken.Application.AppConfig.Queries.GetPlansConfig;
using FluentAssertions;

namespace Awaken.UnitTests.AppConfig;

public class GetPlansConfigQueryHandlerTests
{
    [Fact]
    public async Task HandleReturnsPlansConfigWithTrialDays7()
    {
        var handler = new GetPlansConfigQueryHandler();

        var result = await handler.Handle(new GetPlansConfigQuery("android", "pt-BR"), CancellationToken.None);

        result.TrialDays.Should().Be(7);
        result.Plans.Should().HaveCount(2);
        result.Plans[0].Id.Should().Be("monthly");
        result.Plans[0].PriceLabel.Should().Be("R$ 14,90");
        result.Plans[0].PeriodLabel.Should().Be("/mês");
        result.Plans[0].Highlighted.Should().BeFalse();
        result.Plans[1].Id.Should().Be("annual");
        result.Plans[1].PriceLabel.Should().Be("R$ 99,90");
        result.Plans[1].SavingLabel.Should().Contain("45%");
        result.Plans[1].Highlighted.Should().BeTrue();
    }

    [Theory]
    [InlineData("en", "R$ 14.90", "/month", "R$ 99.90", "/year")]
    [InlineData("es", "R$ 14,90", "/mes", "R$ 99,90", "/año")]
    [InlineData("fr", "R$ 14,90", "/mois", "R$ 99,90", "/an")]
    public async Task HandleReturnsLocalizedPricesByLocale(
        string locale,
        string monthlyPrice,
        string monthlyPeriod,
        string annualPrice,
        string annualPeriod)
    {
        var handler = new GetPlansConfigQueryHandler();

        var result = await handler.Handle(new GetPlansConfigQuery("android", locale), CancellationToken.None);

        result.Plans[0].PriceLabel.Should().Be(monthlyPrice);
        result.Plans[0].PeriodLabel.Should().Be(monthlyPeriod);
        result.Plans[1].PriceLabel.Should().Be(annualPrice);
        result.Plans[1].PeriodLabel.Should().Be(annualPeriod);
    }
}
