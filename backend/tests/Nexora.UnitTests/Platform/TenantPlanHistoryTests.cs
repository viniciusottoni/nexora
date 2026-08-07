using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>US-154 §12 "Unitário: resolução de capacidades e vigência" — cobre <see cref="TenantPlanHistory"/> isolado de banco/HTTP.</summary>
public sealed class TenantPlanHistoryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_Com_Vigencia_Futura_Fica_Pendente()
    {
        var requestedAt = DateTimeOffset.UtcNow;
        var effectiveAt = requestedAt.AddDays(7);

        var history = TenantPlanHistory.Create(TenantId, "GESTAO", "COMPLETO", "Aditivo contratual #32", requestedAt, effectiveAt);

        history.IsPending.Should().BeTrue();
        history.AppliedAt.Should().BeNull();
        history.DomainEventId.Should().BeNull();
        history.PreviousPlan.Should().Be("GESTAO");
        history.NextPlan.Should().Be("COMPLETO");
    }

    [Fact]
    public void Create_Sem_Motivo_Lanca_DomainException()
    {
        var act = () => TenantPlanHistory.Create(TenantId, "GESTAO", "COMPLETO", "  ", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Sem_Plano_Anterior_Lanca_DomainException()
    {
        var act = () => TenantPlanHistory.Create(TenantId, "", "COMPLETO", "motivo", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkApplied_Preenche_AppliedAt_E_DomainEventId_E_Deixa_De_Ser_Pendente()
    {
        var history = TenantPlanHistory.Create(TenantId, "GESTAO", "COMPLETO", "motivo", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var appliedAt = DateTimeOffset.UtcNow.AddMinutes(1);
        var eventId = Guid.NewGuid();

        history.MarkApplied(appliedAt, eventId);

        history.IsPending.Should().BeFalse();
        history.AppliedAt.Should().Be(appliedAt);
        history.DomainEventId.Should().Be(eventId);
    }

    [Fact]
    public void MarkApplied_Chamado_Duas_Vezes_Lanca_DomainException_Idempotencia()
    {
        var history = TenantPlanHistory.Create(TenantId, "GESTAO", "COMPLETO", "motivo", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        history.MarkApplied(DateTimeOffset.UtcNow, Guid.NewGuid());

        var act = () => history.MarkApplied(DateTimeOffset.UtcNow, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }
}
