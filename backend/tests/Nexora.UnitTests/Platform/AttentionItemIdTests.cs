using Nexora.Application.Platform.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-157 — <see cref="AttentionItemId"/> é a chave que <c>POST /v1/platform/attention/{itemId}/acknowledgements</c>
/// decodifica para descobrir o tenant sem consulta prévia (RLS). Cobre round-trip e as formas
/// malformadas que o handler precisa tratar como <c>ATTENTION_ITEM_NOT_FOUND</c>, nunca como exceção.
/// </summary>
public sealed class AttentionItemIdTests
{
    [Fact]
    public void Encode_Decode_E_Round_Trip_Fiel()
    {
        var tenantId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        var encoded = AttentionItemId.Encode(AttentionItemType.InstallationOffline, tenantId, sourceId);
        var decoded = AttentionItemId.TryDecode(encoded);

        decoded.Should().NotBeNull();
        decoded!.Type.Should().Be(AttentionItemType.InstallationOffline);
        decoded.TenantId.Should().Be(tenantId);
        decoded.SourceId.Should().Be(sourceId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("TIPO_DESCONHECIDO|not-a-guid|not-a-guid")]
    [InlineData("INSTALLATION_OFFLINE|not-a-guid")]
    [InlineData("INSTALLATION_OFFLINE")]
    public void TryDecode_Retorna_Nulo_Para_Chaves_Malformadas(string? malformed)
    {
        AttentionItemId.TryDecode(malformed).Should().BeNull();
    }

    [Fact]
    public void TryDecode_Rejeita_Tenant_Guid_Invalido()
    {
        var itemId = $"INVITE_EXPIRED|not-a-guid|{Guid.NewGuid()}";
        AttentionItemId.TryDecode(itemId).Should().BeNull();
    }

    [Fact]
    public void TryDecode_Rejeita_Tipo_Nao_Reconhecido()
    {
        var itemId = $"UNKNOWN_TYPE|{Guid.NewGuid()}|{Guid.NewGuid()}";
        AttentionItemId.TryDecode(itemId).Should().BeNull();
    }
}
