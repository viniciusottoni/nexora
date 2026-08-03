using Nexora.Domain.Common;
using Nexora.Domain.Operation;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>US-030 §12 — item nasce QUEUED (T0) e segue Queued→Fired→InOven→OutOfOven→Ready→Served; transições fora de ordem são proibidas.</summary>
public sealed class OrderItemStateMachineTests
{
    private static OrderItem NewItem() =>
        OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 45.00m);

    [Fact]
    public void Item_Novo_Nasce_Queued()
    {
        var item = NewItem();

        item.Status.Should().Be(OrderItemStatus.Queued);
        item.PlacedAt.Should().NotBe(default);
    }

    /// <summary>US-030 §9 — X-Occurred-At preservado como PlacedAt (T0) do item, não o relógio do servidor.</summary>
    [Fact]
    public void Create_Preserva_O_OccurredAt_Informado_Como_PlacedAt()
    {
        var occurredAt = new DateTimeOffset(2026, 7, 31, 20, 3, 0, TimeSpan.Zero);

        var item = OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 45.00m, occurredAt: occurredAt);

        item.PlacedAt.Should().Be(occurredAt);
    }

    [Fact]
    public void Fire_A_Partir_De_Queued_E_Permitido()
    {
        var item = NewItem();
        var actor = Guid.NewGuid();

        item.Fire(actor);

        item.Status.Should().Be(OrderItemStatus.Fired);
        item.FiredBy.Should().Be(actor);
    }

    [Fact]
    public void Fire_Sobre_Item_Ja_Disparado_E_Proibido()
    {
        var item = NewItem();
        item.Fire(Guid.NewGuid());

        var act = () => item.Fire(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SendToOven_Sem_Ter_Sido_Disparado_E_Proibido()
    {
        var item = NewItem();

        var act = () => item.SendToOven(ovenSlot: null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkServed_Sem_Estar_Pronto_E_Proibido()
    {
        var item = NewItem();
        item.Fire(Guid.NewGuid());

        var act = () => item.MarkServed(Guid.NewGuid());

        act.Should().Throw<DomainException>("só é possível servir um item que já está READY");
    }

    [Fact]
    public void Cancel_De_Item_Ja_Servido_E_Proibido()
    {
        var item = NewItem();
        item.Fire(Guid.NewGuid());
        item.SendToOven(ovenSlot: null);
        item.TakeOutOfOven();
        item.MarkReady(Guid.NewGuid());
        item.MarkServed(Guid.NewGuid());

        var act = () => item.Cancel("Motivo qualquer", Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Fluxo_Completo_Ate_Served_Segue_A_Ordem_Esperada()
    {
        var item = NewItem();

        item.Fire(Guid.NewGuid());
        item.SendToOven(ovenSlot: 3);
        item.TakeOutOfOven();
        item.MarkReady(Guid.NewGuid());
        item.MarkServed(Guid.NewGuid());

        item.Status.Should().Be(OrderItemStatus.Served);
    }
}
