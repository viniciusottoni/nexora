using Nexora.Domain.Catalog;
using Nexora.Domain.Common;
using Nexora.Domain.Operation;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>
/// US-030 §12 ("Máquina de estados: transições válidas e proibidas do documento 04") — cobre a
/// criação (<c>Draft</c>→<c>Placed</c>, T0) e as transições proibidas mais relevantes para esta
/// história (não é o escopo completo do ciclo de vida do pedido, que outras histórias — US-031,
/// US-033 — também exercitam).
/// </summary>
public sealed class OrderStateMachineTests
{
    private static Order NewDraftOrder() =>
        Order.Create(Guid.NewGuid(), Guid.NewGuid(), Channel.DineIn, "A1", DateOnly.FromDateTime(DateTime.UtcNow));

    [Fact]
    public void Pedido_Novo_Nasce_Em_Draft()
    {
        var order = NewDraftOrder();

        order.Status.Should().Be(OrderStatus.Draft);
        order.PlacedAt.Should().BeNull();
    }

    /// <summary>Cenário Gherkin "Pedido do cliente na mesa" (US-030 §4): confirmar o pedido leva a PLACED com T0 gravado.</summary>
    [Fact]
    public void Place_A_Partir_De_Draft_Confirma_O_Pedido_E_Grava_PlacedAt()
    {
        var order = NewDraftOrder();

        order.Place();

        order.Status.Should().Be(OrderStatus.Placed);
        order.PlacedAt.Should().NotBeNull();
    }

    /// <summary>US-030 §9 (comportamento offline) — X-Occurred-At preservado como T0, não o relógio do servidor.</summary>
    [Fact]
    public void Place_Preserva_O_OccurredAt_Informado_Em_Vez_Do_Relogio_Do_Servidor()
    {
        var order = NewDraftOrder();
        var occurredAt = new DateTimeOffset(2026, 7, 31, 20, 3, 0, TimeSpan.Zero);

        order.Place(occurredAt);

        order.PlacedAt.Should().Be(occurredAt);
    }

    [Fact]
    public void Place_Sobre_Pedido_Ja_Confirmado_E_Proibido()
    {
        var order = NewDraftOrder();
        order.Place();

        var act = () => order.Place();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void StartProduction_Sem_Ter_Sido_Confirmado_E_Proibido()
    {
        var order = NewDraftOrder();

        var act = order.StartProduction;

        act.Should().Throw<DomainException>("um pedido em rascunho não pode ir direto para produção, precisa passar por PLACED");
    }

    [Fact]
    public void MarkReady_Sem_Ter_Entrado_Em_Producao_E_Proibido()
    {
        var order = NewDraftOrder();
        order.Place();

        var act = order.MarkReady;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_De_Pedido_Ja_Cancelado_E_Proibido()
    {
        var order = NewDraftOrder();
        order.Place();
        order.Cancel("Cliente desistiu", Guid.NewGuid());

        var act = () => order.Cancel("Segunda tentativa", Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddItem_Aceita_Item_Enquanto_O_Pedido_Esta_Em_Draft()
    {
        var order = NewDraftOrder();
        var item = OrderItem.Create(order.TenantId, order.Id, Guid.NewGuid(), 45.00m);

        order.AddItem(item);

        order.Items.Should().ContainSingle();
        item.Status.Should().Be(OrderItemStatus.Queued, "cada item nasce QUEUED — US-030 §4, cenário 'Pedido do cliente na mesa'");
    }
}
