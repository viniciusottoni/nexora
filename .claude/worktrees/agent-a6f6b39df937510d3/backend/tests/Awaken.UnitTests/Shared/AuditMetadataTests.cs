using Awaken.Shared.Audit;
using FluentAssertions;
using System.Text.Json;

namespace Awaken.UnitTests.Shared;

public class AuditMetadataTests
{
    [Fact]
    public void Safe_WithNormalData_ProducesValidJson()
    {
        var result = AuditMetadata.Safe(new { plan = "monthly", accessStatus = "subscription_active" });

        var doc = JsonDocument.Parse(result); // throws if invalid JSON
        doc.RootElement.GetProperty("plan").GetString().Should().Be("monthly");
        doc.RootElement.GetProperty("accessStatus").GetString().Should().Be("subscription_active");
    }

    [Fact]
    public void Safe_ExcludesKnownSensitiveKeys_Token()
    {
        var result = AuditMetadata.Safe(new { reason = "shop_purchase", token = "secret-token-value" });

        var doc = JsonDocument.Parse(result);
        doc.RootElement.TryGetProperty("token", out _).Should().BeFalse("token is a sensitive key");
        doc.RootElement.GetProperty("reason").GetString().Should().Be("shop_purchase");
    }

    [Fact]
    public void Safe_ExcludesKnownSensitiveKeys_Password()
    {
        var result = AuditMetadata.Safe(new { userId = "123", password = "hunter2" });

        var doc = JsonDocument.Parse(result);
        doc.RootElement.TryGetProperty("password", out _).Should().BeFalse("password is a sensitive key");
        doc.RootElement.GetProperty("userId").GetString().Should().Be("123");
    }

    [Fact]
    public void Safe_ExcludesKnownSensitiveKeys_ReceiptData()
    {
        var result = AuditMetadata.Safe(new
        {
            productKey = "item_premium_card",
            channel = "iap",
            receiptData = "eyJhbGciOiJSUzI1NiJ9..."
        });

        var doc = JsonDocument.Parse(result);
        doc.RootElement.TryGetProperty("receiptData", out _).Should().BeFalse("receiptData is a sensitive key");
        doc.RootElement.GetProperty("productKey").GetString().Should().Be("item_premium_card");
    }

    [Fact]
    public void Safe_WithSpecialCharactersInValues_ProducesValidJson()
    {
        var result = AuditMetadata.Safe(new { reason = "shop\"purchase", channel = "gold & iap" });

        var act = () => JsonDocument.Parse(result);
        act.Should().NotThrow("special characters must be escaped by the serializer");
    }

    [Fact]
    public void Safe_WithNullProperty_HandlesGracefully()
    {
        var result = AuditMetadata.Safe(new { plan = (string?)null, accessStatus = "no_subscription" });

        var doc = JsonDocument.Parse(result);
        // null values serialize as JSON null
        doc.RootElement.GetProperty("plan").ValueKind.Should().Be(JsonValueKind.Null);
        doc.RootElement.GetProperty("accessStatus").GetString().Should().Be("no_subscription");
    }

    [Fact]
    public void Safe_IsCaseInsensitiveForSensitiveKeys()
    {
        var result = AuditMetadata.Safe(new { Token = "upper-case-token", reason = "test" });

        var doc = JsonDocument.Parse(result);
        // "Token" (PascalCase) should be excluded because SensitiveKeys uses OrdinalIgnoreCase
        doc.RootElement.TryGetProperty("Token", out _).Should().BeFalse("sensitive key check is case-insensitive");
        doc.RootElement.GetProperty("reason").GetString().Should().Be("test");
    }

    [Fact]
    public void Safe_WithEmptyObject_ReturnsEmptyJsonObject()
    {
        var result = AuditMetadata.Safe(new { });

        result.Should().Be("{}");
    }
}
