using Nexora.Domain.Common;
using Nexora.Domain.Operation;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>Regras de negócio de <see cref="TableSession"/> introduzidas pela US-021/US-022.</summary>
public sealed class TableSessionTests
{
    private static (Guid TenantId, Guid StoreId, Guid TableId) Ids() => (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Create_Sem_Pessoas_Lanca_DomainException()
    {
        var (tenantId, storeId, tableId) = Ids();

        var act = () => TableSession.Create(tenantId, storeId, tableId, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 0);

        act.Should().Throw<DomainException>();
    }

    /// <summary>Cenário Gherkin "Abertura pelo garçom" (US-022): fonte WAITER nasce com a contagem já confirmada.</summary>
    [Fact]
    public void Create_Com_Origem_Waiter_Nasce_Com_Contagem_Confirmada()
    {
        var (tenantId, storeId, tableId) = Ids();

        var session = TableSession.Create(
            tenantId, storeId, tableId, DateOnly.FromDateTime(DateTime.UtcNow),
            guestCount: 4, openedSource: "WAITER", guestCountConfirmed: true);

        session.OpenedSource.Should().Be("WAITER");
        session.GuestCountConfirmed.Should().BeTrue();
    }

    /// <summary>Cenário Gherkin "Abertura pelo cliente via QR" (US-022 §4 e US-021 §4 "Primeira leitura da mesa").</summary>
    [Fact]
    public void Create_Com_Origem_Qr_Nasce_Com_Contagem_Pendente_De_Confirmacao()
    {
        var (tenantId, storeId, tableId) = Ids();

        var session = TableSession.Create(
            tenantId, storeId, tableId, DateOnly.FromDateTime(DateTime.UtcNow),
            guestCount: 1, openedSource: "QR", guestCountConfirmed: false);

        session.OpenedSource.Should().Be("QR");
        session.GuestCountConfirmed.Should().BeFalse();
    }

    /// <summary>RN-020 (US-022 §9): o horário informado por X-Occurred-At é preservado, não o relógio do servidor.</summary>
    [Fact]
    public void Create_Com_OccurredAt_Preserva_O_Horario_Informado()
    {
        var (tenantId, storeId, tableId) = Ids();
        var occurredAt = DateTimeOffset.UtcNow.AddHours(-3);

        var session = TableSession.Create(
            tenantId, storeId, tableId, DateOnly.FromDateTime(occurredAt.UtcDateTime),
            guestCount: 2, occurredAt: occurredAt);

        session.OpenedAt.Should().Be(occurredAt);
    }

    [Fact]
    public void Create_Sem_OccurredAt_Usa_O_Relogio_Do_Servidor()
    {
        var (tenantId, storeId, tableId) = Ids();
        var before = DateTimeOffset.UtcNow;

        var session = TableSession.Create(tenantId, storeId, tableId, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2);

        session.OpenedAt.Should().BeOnOrAfter(before);
    }

    /// <summary>Confirmação da contagem pendente (US-021): o garçom informa e a sessão passa a "confirmada".</summary>
    [Fact]
    public void UpdateGuestCount_Confirma_Contagem_Pendente_De_Sessao_Aberta_Por_Qr()
    {
        var (tenantId, storeId, tableId) = Ids();
        var session = TableSession.Create(
            tenantId, storeId, tableId, DateOnly.FromDateTime(DateTime.UtcNow),
            guestCount: 1, openedSource: "QR", guestCountConfirmed: false);

        session.UpdateGuestCount(4);

        session.GuestCount.Should().Be(4);
        session.GuestCountConfirmed.Should().BeTrue();
    }

    [Fact]
    public void UpdateGuestCount_Com_Zero_Lanca_DomainException()
    {
        var (tenantId, storeId, tableId) = Ids();
        var session = TableSession.Create(tenantId, storeId, tableId, DateOnly.FromDateTime(DateTime.UtcNow));

        var act = () => session.UpdateGuestCount(0);

        act.Should().Throw<DomainException>();
    }

    /// <summary>Cenário Gherkin "Troca de garçom responsável" (US-022 §4).</summary>
    [Fact]
    public void ReassignWaiter_Troca_O_Responsavel_Atual()
    {
        var (tenantId, storeId, tableId) = Ids();
        var joao = Guid.NewGuid();
        var ana = Guid.NewGuid();
        var session = TableSession.Create(tenantId, storeId, tableId, DateOnly.FromDateTime(DateTime.UtcNow), waiterId: joao);

        session.WaiterId.Should().Be(joao);

        session.ReassignWaiter(ana);

        session.WaiterId.Should().Be(ana);
        session.WaiterId.Should().NotBe(joao);
    }
}
