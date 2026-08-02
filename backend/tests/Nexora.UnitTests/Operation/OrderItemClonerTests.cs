using Nexora.Application.Orders.Support;
using Nexora.Domain.Operation;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>
/// US-028 §12 (Estratégia de teste): "Unitário — Cópia fiel de frações, modificadores e
/// observações". <see cref="OrderItemCloner"/> é a extração pura (sem <c>IApplicationDbContext</c>)
/// da composição de um <see cref="OrderItem"/> de origem — testável em unidade, sem
/// Testcontainers.
/// </summary>
public sealed class OrderItemClonerTests
{
    private static (Guid TenantId, Guid OrderId, Guid VariantId) Ids() => (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void CopyModifiers_Devolve_Mesmo_ModifierId_E_Quantidade_Do_Original()
    {
        var (tenantId, orderId, variantId) = Ids();
        var item = OrderItem.Create(tenantId, orderId, variantId, unitPrice: 30m);
        var modifierAId = Guid.NewGuid();
        var modifierBId = Guid.NewGuid();
        item.AddModifier(OrderItemModifier.Create(tenantId, item.Id, modifierAId, "Bacon extra", priceDelta: 5m, quantity: 2));
        item.AddModifier(OrderItemModifier.Create(tenantId, item.Id, modifierBId, "Sem cebola", priceDelta: 0m, quantity: 1));

        var copied = OrderItemCloner.CopyModifiers(item);

        copied.Should().HaveCount(2);
        copied.Should().ContainSingle(m => m.ModifierId == modifierAId && m.Quantity == 2);
        copied.Should().ContainSingle(m => m.ModifierId == modifierBId && m.Quantity == 1);
    }

    [Fact]
    public void CopyFractions_Devolve_Mesmo_VariantId_E_Peso_Do_Original_Meio_A_Meio()
    {
        var (tenantId, orderId, variantId) = Ids();
        var item = OrderItem.Create(tenantId, orderId, variantId, unitPrice: 60m);
        var sabor1 = Guid.NewGuid();
        var sabor2 = Guid.NewGuid();
        item.AddFraction(OrderItemFraction.Create(tenantId, item.Id, sabor1, weight: 0.5m, unitPrice: 55m, sortOrder: 0));
        item.AddFraction(OrderItemFraction.Create(tenantId, item.Id, sabor2, weight: 0.5m, unitPrice: 65m, sortOrder: 1));

        var copied = OrderItemCloner.CopyFractions(item);

        copied.Should().HaveCount(2);
        copied[0].VariantId.Should().Be(sabor1);
        copied[0].Weight.Should().Be(0.5m);
        copied[1].VariantId.Should().Be(sabor2);
        copied[1].Weight.Should().Be(0.5m);
    }

    [Fact]
    public void CopyNotes_Devolve_A_Mesma_Observacao_Do_Original()
    {
        var (tenantId, orderId, variantId) = Ids();
        var item = OrderItem.Create(tenantId, orderId, variantId, unitPrice: 12m, notes: "Sem gelo, por favor");

        OrderItemCloner.CopyNotes(item).Should().Be("Sem gelo, por favor");
    }

    [Fact]
    public void CopyNotes_Com_Item_Sem_Observacao_Devolve_Nulo()
    {
        var (tenantId, orderId, variantId) = Ids();
        var item = OrderItem.Create(tenantId, orderId, variantId, unitPrice: 12m);

        OrderItemCloner.CopyNotes(item).Should().BeNull();
    }

    [Fact]
    public void CopyFractions_Sem_Nenhuma_Fracao_Devolve_Lista_Vazia()
    {
        var (tenantId, orderId, variantId) = Ids();
        var item = OrderItem.Create(tenantId, orderId, variantId, unitPrice: 12m);

        OrderItemCloner.CopyFractions(item).Should().BeEmpty();
    }
}
