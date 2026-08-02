using Nexora.Domain.Catalog;
using Nexora.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-011 (Variações de produto com preço próprio) §15 ("Modelar preço direto na variação, sem
/// tabela historizada, inviabilizaria o recálculo de margem histórica") — cobre as invariantes de
/// <see cref="Price"/> isoladas de banco/HTTP: é imutável por design, uma "troca de preço" é
/// sempre fechar a linha vigente (<see cref="Price.Close"/>) e criar uma nova (<see cref="Price.Create"/>),
/// nunca editar o valor de uma linha existente. O fluxo completo de historização via
/// <c>SetVariantPriceCommandHandler</c> é coberto em <c>Nexora.IntegrationTests.CatalogIntegrationTests</c>.
/// </summary>
public sealed class PriceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();

    [Fact]
    public void Create_Com_Valor_Negativo_Lanca_DomainException()
    {
        var act = () => Price.Create(TenantId, VariantId, Channel.DineIn, amount: -0.01m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Aceita_Valor_Zero()
    {
        var act = () => Price.Create(TenantId, VariantId, Channel.DineIn, amount: 0m);

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_Preenche_Canal_Valor_E_ValidFrom_Padrao_Agora()
    {
        var before = DateTimeOffset.UtcNow;

        var price = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m);

        price.TenantId.Should().Be(TenantId);
        price.VariantId.Should().Be(VariantId);
        price.Channel.Should().Be(Channel.DineIn);
        price.Amount.Should().Be(45.00m);
        price.ValidFrom.Should().BeOnOrAfter(before);
        price.ValidTo.Should().BeNull("um preço recém-criado ainda está vigente");
    }

    [Fact]
    public void Create_Aceita_ValidFrom_Explicito()
    {
        var validFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var price = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m, validFrom: validFrom);

        price.ValidFrom.Should().Be(validFrom);
    }

    [Fact]
    public void Close_Encerra_A_Vigencia_Preenchendo_ValidTo()
    {
        var price = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m);
        var validTo = DateTimeOffset.UtcNow;

        price.Close(validTo);

        price.ValidTo.Should().Be(validTo);
    }

    [Fact]
    public void Close_Chamado_Duas_Vezes_Lanca_DomainException()
    {
        var price = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m);
        price.Close(DateTimeOffset.UtcNow);

        var act = () => price.Close(DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>("um preço já encerrado não pode ser encerrado de novo — a mudança de preço sempre cria uma linha nova (US-011 §15)");
    }
    [Fact]
    public void Close_No_Instante_Ou_Antes_Do_Inicio_Lanca_DomainException()
    {
        var validFrom = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var atStart = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m, validFrom: validFrom);
        var beforeStart = Price.Create(TenantId, VariantId, Channel.DineIn, 45.00m, validFrom: validFrom);

        var closeAtStart = () => atStart.Close(validFrom);
        var closeBeforeStart = () => beforeStart.Close(validFrom.AddTicks(-1));

        closeAtStart.Should().Throw<DomainException>();
        closeBeforeStart.Should().Throw<DomainException>();
    }
}
