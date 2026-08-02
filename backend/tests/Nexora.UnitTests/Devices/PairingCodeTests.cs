using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Devices;

/// <summary>
/// US-005 §12 ("Unitário: ... expiração e uso único do código de pareamento") — o gerador
/// (<see cref="PairingCodeGeneratorTests"/>) só produz o dígito; expiração e consumo de uso único
/// são regra de <see cref="PairingCode"/> (porta de <c>device-registry.ts</c>), verificada aqui
/// isolada de banco/HTTP.
/// </summary>
public sealed class PairingCodeTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid CreatedBy = Guid.NewGuid();

    [Fact]
    public void Create_Com_CodeHash_Vazio_Lanca_DomainException()
    {
        var act = () => PairingCode.Create(TenantId, StoreId, string.Empty, CreatedBy, DateTimeOffset.UtcNow.AddMinutes(10));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsExpired_Antes_Do_Prazo_De_Dez_Minutos_E_Falso()
    {
        var now = DateTimeOffset.UtcNow;
        var pairingCode = PairingCode.Create(TenantId, StoreId, "hash", CreatedBy, now.AddMinutes(10));

        pairingCode.IsExpired(now.AddMinutes(9).AddSeconds(59)).Should().BeFalse();
    }

    /// <summary>Cenário Gherkin "Código expirado" da US-005: no instante exato de expiração, o código já é recusado.</summary>
    [Fact]
    public void IsExpired_No_Instante_Exato_De_Expiracao_E_Verdadeiro()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(10);
        var pairingCode = PairingCode.Create(TenantId, StoreId, "hash", CreatedBy, expiresAt);

        pairingCode.IsExpired(expiresAt).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Apos_Dez_Minutos_E_Verdadeiro()
    {
        var now = DateTimeOffset.UtcNow;
        var pairingCode = PairingCode.Create(TenantId, StoreId, "hash", CreatedBy, now.AddMinutes(10));

        pairingCode.IsExpired(now.AddMinutes(10).AddSeconds(1)).Should().BeTrue();
    }

    /// <summary>Cenário Gherkin "Pareamento de novo terminal": consumir marca o código como usado e invalida reuso.</summary>
    [Fact]
    public void Consume_Marca_Como_Consumido()
    {
        var pairingCode = PairingCode.Create(TenantId, StoreId, "hash", CreatedBy, DateTimeOffset.UtcNow.AddMinutes(10));

        pairingCode.IsConsumed.Should().BeFalse();
        pairingCode.Consume();

        pairingCode.IsConsumed.Should().BeTrue();
        pairingCode.ConsumedAt.Should().NotBeNull();
    }

    /// <summary>Uso único: consumir um código já consumido é recusado, nunca silenciosamente ignorado.</summary>
    [Fact]
    public void Consume_De_Codigo_Ja_Consumido_Lanca_DomainException()
    {
        var pairingCode = PairingCode.Create(TenantId, StoreId, "hash", CreatedBy, DateTimeOffset.UtcNow.AddMinutes(10));
        pairingCode.Consume();

        var act = () => pairingCode.Consume();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordAttempt_Incrementa_Contador_De_Tentativas_Para_Rate_Limit()
    {
        var pairingCode = PairingCode.Create(TenantId, StoreId, "hash", CreatedBy, DateTimeOffset.UtcNow.AddMinutes(10));

        pairingCode.RecordAttempt();
        pairingCode.RecordAttempt();

        pairingCode.Attempts.Should().Be(2);
    }
}
