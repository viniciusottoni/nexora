using System.Text.Json;
using FluentAssertions;
using Nexora.Contracts.Catalog;
using Xunit;

namespace Nexora.ApiTests.Catalog;

public sealed class VariantJsonContractTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Variante_Serializa_Preco_Como_String()
    {
        var response = new VariantResponse(
            Guid.NewGuid(), Guid.NewGuid(), "Grande", null, "G", 0, true, true, 45.90m, "DineIn");

        var json = JsonSerializer.Serialize(response, WebJson);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("currentPrice").ValueKind.Should().Be(JsonValueKind.String);
        document.RootElement.GetProperty("currentPrice").GetString().Should().Be("45.90");
    }

    [Fact]
    public void Criacao_De_Variante_Aceita_Preco_Como_String()
    {
        const string json =
            """{"name":"Grande","sizeCode":"G","sku":null,"prepMinutes":null,"isDefault":true,"basePrice":"45.90","channel":"DineIn"}""";

        var request = JsonSerializer.Deserialize<CreateVariantRequest>(json, WebJson);

        request.Should().NotBeNull();
        request!.BasePrice.Should().Be(45.90m);
    }
}
