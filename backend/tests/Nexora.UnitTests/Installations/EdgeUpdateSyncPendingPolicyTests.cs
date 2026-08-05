using Nexora.Application.Installations.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Installations;

/// <summary>US-146 §4 "Instalação com pendência de sincronização" — decisão pura de adiamento por volume de eventos pendentes.</summary>
public sealed class EdgeUpdateSyncPendingPolicyTests
{
    [Fact]
    public void Abaixo_Do_Limiar_Nao_Adia()
    {
        EdgeUpdateSyncPendingPolicy.ShouldDefer(EdgeUpdateSyncPendingPolicy.PendingEventsThreshold - 1).Should().BeFalse();
    }

    [Fact]
    public void No_Limiar_Ou_Acima_Adia()
    {
        EdgeUpdateSyncPendingPolicy.ShouldDefer(EdgeUpdateSyncPendingPolicy.PendingEventsThreshold).Should().BeTrue();
        EdgeUpdateSyncPendingPolicy.ShouldDefer(EdgeUpdateSyncPendingPolicy.PendingEventsThreshold + 1).Should().BeTrue();
    }

    [Fact]
    public void Zero_Eventos_Pendentes_Nao_Adia()
    {
        EdgeUpdateSyncPendingPolicy.ShouldDefer(0).Should().BeFalse();
    }
}
