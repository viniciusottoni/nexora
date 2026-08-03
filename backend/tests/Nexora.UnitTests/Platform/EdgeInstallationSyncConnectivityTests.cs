using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-034 §6/§12 ("Unitário": a regra pura de "transicionou de X para Y" — sem banco, sem
/// DomainEvent/SignalR, só a decisão que <see cref="EdgeInstallation.RecordHeartbeat"/> toma ao
/// comparar o estado anterior com o observado neste poll). O que acontece DEPOIS da transição
/// (gravar EVT-083/EVT-084, propagar sync.status) é responsabilidade da Application — coberto por
/// <c>Nexora.IntegrationTests.PollSyncHealthIntegrationTests</c>.
/// </summary>
public sealed class EdgeInstallationSyncConnectivityTests
{
    private static EdgeInstallation CreateInstallation() =>
        EdgeInstallation.Create(Guid.NewGuid(), Guid.NewGuid(), "Servidor local — Matriz");

    [Fact]
    public void Primeiro_Poll_Unknown_Para_Online_Nao_E_Transicao()
    {
        var installation = CreateInstallation();
        installation.Connectivity.Should().Be(SyncConnectivity.Unknown);

        var transition = installation.RecordHeartbeat(0, null, SyncConnectivity.Online);

        transition.Kind.Should().Be(SyncTransitionKind.None, "não houve queda nenhuma para 'retornar' de");
        installation.Connectivity.Should().Be(SyncConnectivity.Online);
        installation.OfflineSince.Should().BeNull();
    }

    [Fact]
    public void Primeiro_Poll_Unknown_Para_Offline_Tambem_Dispara_WentOffline()
    {
        var installation = CreateInstallation();

        var transition = installation.RecordHeartbeat(0, null, SyncConnectivity.Offline);

        // [DECISÃO] Unknown -> Offline É tratado como queda (diferente de Unknown -> Online, que
        // nunca é "retorno"): a primeira vez que o edge observa a nuvem inalcançável já é
        // informação nova e relevante para o operador — não há razão para engolir o primeiro sinal.
        transition.Kind.Should().Be(SyncTransitionKind.WentOffline);
        installation.Connectivity.Should().Be(SyncConnectivity.Offline);
        installation.OfflineSince.Should().NotBeNull();
    }

    [Fact]
    public void Online_Para_Offline_Dispara_WentOffline_E_Marca_OfflineSince()
    {
        var installation = CreateInstallation();
        installation.RecordHeartbeat(0, null, SyncConnectivity.Online);

        var transition = installation.RecordHeartbeat(0, null, SyncConnectivity.Offline);

        transition.Kind.Should().Be(SyncTransitionKind.WentOffline);
        transition.OfflineSince.Should().NotBeNull();
        transition.OfflineDuration.Should().BeNull("a duração só é conhecida quando a reconexão acontece, não na queda");
        installation.Connectivity.Should().Be(SyncConnectivity.Offline);
        installation.OfflineSince.Should().Be(transition.OfflineSince);
    }

    [Fact]
    public void Offline_Para_Online_Dispara_Reconnected_Com_Duracao_E_Limpa_OfflineSince()
    {
        var installation = CreateInstallation();
        installation.RecordHeartbeat(0, null, SyncConnectivity.Online);
        installation.RecordHeartbeat(0, null, SyncConnectivity.Offline);
        var offlineSince = installation.OfflineSince;

        var transition = installation.RecordHeartbeat(0, null, SyncConnectivity.Online);

        transition.Kind.Should().Be(SyncTransitionKind.Reconnected);
        transition.OfflineSince.Should().Be(offlineSince);
        transition.OfflineDuration.Should().NotBeNull();
        transition.OfflineDuration!.Value.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        installation.Connectivity.Should().Be(SyncConnectivity.Online);
        installation.OfflineSince.Should().BeNull();
    }

    [Fact]
    public void Poll_Repetido_Com_Mesmo_Estado_Nunca_Dispara_Transicao()
    {
        var installation = CreateInstallation();
        installation.RecordHeartbeat(0, null, SyncConnectivity.Online);

        var second = installation.RecordHeartbeat(0, null, SyncConnectivity.Online);
        var third = installation.RecordHeartbeat(0, null, SyncConnectivity.Online);

        second.Kind.Should().Be(SyncTransitionKind.None);
        third.Kind.Should().Be(SyncTransitionKind.None);
    }

    [Fact]
    public void Observacao_Unknown_Nunca_Dispara_Transicao_Nem_Muda_O_Estado_Atual()
    {
        var installation = CreateInstallation();
        installation.RecordHeartbeat(0, null, SyncConnectivity.Offline);

        var transition = installation.RecordHeartbeat(0, null, SyncConnectivity.Unknown);

        transition.Kind.Should().Be(SyncTransitionKind.None);
        installation.Connectivity.Should().Be(SyncConnectivity.Offline, "um poll sem sinal real (Unknown) não pode apagar o último estado conhecido");
        installation.OfflineSince.Should().NotBeNull();
    }

    [Fact]
    public void RecordHeartbeat_Sempre_Atualiza_LastSeenAt_E_LastSyncedSeq_Independente_De_Transicao()
    {
        var installation = CreateInstallation();

        installation.RecordHeartbeat(42, clockOffsetMs: 15, SyncConnectivity.Online);

        installation.LastSeenAt.Should().NotBeNull();
        installation.LastSyncedSeq.Should().Be(42);
        installation.ClockOffsetMs.Should().Be(15);
    }
}
