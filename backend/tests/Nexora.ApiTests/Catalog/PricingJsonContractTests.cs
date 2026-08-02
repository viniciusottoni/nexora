using System.Text.Json;
using Nexora.Contracts.Catalog;
using FluentAssertions;
using Xunit;

namespace Nexora.ApiTests.Catalog;

public sealed class PricingJsonContractTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Tabela_De_Precos_Serializa_Dinheiro_Como_String()
    {
        var response = new VariantPriceTableResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new[]
            {
                new VariantChannelPriceRow("DineIn", 45.00m, false, DateTimeOffset.UtcNow),
            });

        var json = JsonSerializer.Serialize(response, WebJson);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("channels")[0].GetProperty("amount").ValueKind
            .Should().Be(JsonValueKind.String);
        document.RootElement.GetProperty("channels")[0].GetProperty("amount").GetString()
            .Should().Be("45.00");
    }

    [Fact]
    public void Alteracao_De_Preco_Aceita_Dinheiro_Como_String()
    {
        const string json = """{"prices":[{"channel":"Delivery","amount":"52.00"}]}""";

        var request = JsonSerializer.Deserialize<SetVariantChannelPriceRequest>(json, WebJson);

        request.Should().NotBeNull();
        request!.Prices.Should().ContainSingle();
        request.Prices[0].Amount.Should().Be(52.00m);
    }

    [Fact]
    public void Cardapio_Publico_Serializa_Preco_Como_String()
    {
        var response = new PublicMenuProductResponse(
            Guid.NewGuid(), "Pizza", null, null, Array.Empty<string>(), null, 0, 45.00m);

        var json = JsonSerializer.Serialize(response, WebJson);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("fromPrice").ValueKind.Should().Be(JsonValueKind.String);
        document.RootElement.GetProperty("fromPrice").GetString().Should().Be("45.00");
    }
}
