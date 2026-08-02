using Nexora.Domain.Catalog;
using Nexora.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-015 (Marcar produto indisponível com propagação imediata) — cobre as invariantes de
/// <see cref="Product.MarkUnavailable"/>/<see cref="Product.MarkAvailable"/> isoladas de banco/HTTP,
/// que a suíte de US-010 (<c>ProductTests</c>) deliberadamente não cobre (ver docstring lá: "são
/// escopo da US-015"). Este arquivo é NOVO neste worktree — a suíte <c>ProductTests.cs</c> de
/// US-010 não existe aqui (ver relatório da tarefa sobre o estado deste worktree isolado), então
/// não há risco de sobreposição/duplicata de teste.
/// </summary>
public sealed class ProductAvailabilityTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void MarkUnavailable_Com_Motivo_Vazio_Lanca_DomainException()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Calabresa");

        var act = () => product.MarkUnavailable("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkUnavailable_Preenche_Motivo_E_Marca_O_Instante()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Calabresa");
        var before = DateTimeOffset.UtcNow;

        product.MarkUnavailable("Acabou a calabresa");

        product.IsAvailable.Should().BeFalse();
        product.UnavailableReason.Should().Be("Acabou a calabresa");
        product.UnavailableSince.Should().NotBeNull();
        product.UnavailableSince!.Value.Should().BeOnOrAfter(before);
        product.AutoRestoreNextDay.Should().BeTrue();
    }

    [Fact]
    public void MarkUnavailable_Duas_Vezes_Com_Motivos_Diferentes_Atualiza_O_Motivo()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Calabresa");

        product.MarkUnavailable("Acabou o insumo");
        var firstUnavailableSince = product.UnavailableSince;
        product.MarkUnavailable("Praça fechada");

        product.IsAvailable.Should().BeFalse();
        product.UnavailableReason.Should().Be("Praça fechada", "o motivo mais recente prevalece — cenário de marcação repetida");
        product.UnavailableSince.Should().NotBeNull();
        product.UnavailableSince.Should().BeOnOrAfter(firstUnavailableSince!.Value);
    }

    [Fact]
    public void MarkAvailable_Limpa_Motivo_E_Instante_De_Indisponibilidade()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Calabresa");
        product.MarkUnavailable("Acabou o insumo");

        product.MarkAvailable();

        product.IsAvailable.Should().BeTrue();
        product.UnavailableReason.Should().BeNull();
        product.UnavailableSince.Should().BeNull();
        product.AutoRestoreNextDay.Should().BeTrue();
    }

    [Fact]
    public void MarkAvailable_Em_Produto_Ja_Disponivel_E_Um_No_Op_Seguro()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Calabresa");

        var act = () => product.MarkAvailable();

        act.Should().NotThrow();
        product.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Produto_Recem_Criado_Nasce_Disponivel_Sem_Motivo_De_Indisponibilidade()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Calabresa");

        product.IsAvailable.Should().BeTrue();
        product.UnavailableReason.Should().BeNull();
        product.UnavailableSince.Should().BeNull();
        product.AutoRestoreNextDay.Should().BeTrue();
    }

    [Fact]
    public void MarkUnavailable_Sem_Retorno_Automatico_Preserva_A_Opcao()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Calabresa");

        product.MarkUnavailable("Produto sazonal", autoRestoreNextDay: false);

        product.IsAvailable.Should().BeFalse();
        product.AutoRestoreNextDay.Should().BeFalse();
    }
}
