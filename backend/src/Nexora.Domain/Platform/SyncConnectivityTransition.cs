namespace Nexora.Domain.Platform;

/// <summary>
/// Estado de conectividade do edge com a nuvem, tal como observado pelo último ping de saúde
/// (US-034 §6/§7). <see cref="Unknown"/> é o estado inicial (nunca poll bem-sucedido nem
/// definitivamente falho ainda — ex.: <c>EdgeInstallationIdentityOptions.SyncEndpoint</c> não
/// configurado em ambiente de desenvolvimento/teste) e nunca dispara transição — só a alternância
/// real entre <see cref="Online"/> e <see cref="Offline"/> é que importa para
/// <see cref="EdgeInstallation.RecordHeartbeat"/>.
/// </summary>
public enum SyncConnectivity
{
    Unknown,
    Online,
    Offline
}

/// <summary>Resultado da comparação feita por <see cref="EdgeInstallation.RecordHeartbeat"/> entre o estado anterior e o observado.</summary>
public enum SyncTransitionKind
{
    /// <summary>Nenhuma mudança de estado — a maioria dos heartbeats (ADR-007, poll a cada poucos segundos).</summary>
    None,

    /// <summary>EVT-083 <c>edge.offline_detected</c> — a nuvem estava alcançável (ou nunca observada) e agora não está.</summary>
    WentOffline,

    /// <summary>EVT-084 <c>edge.reconnected</c> — a nuvem estava inalcançável e voltou a responder.</summary>
    Reconnected
}

/// <summary>
/// Resultado de <see cref="EdgeInstallation.RecordHeartbeat"/> — devolvido ao chamador (Application)
/// para decidir se grava <c>DomainEvent</c>/propaga <c>sync.status</c> (US-034 §6/§7). O Domain só
/// decide A REGRA de transição (comparar estado anterior x observado); ele não sabe nada de
/// DomainEvent/SignalR — isso é responsabilidade do handler, mantendo ADR-039 intacto.
/// </summary>
public sealed record SyncConnectivityTransition(SyncTransitionKind Kind, DateTimeOffset? OfflineSince, TimeSpan? OfflineDuration)
{
    public static readonly SyncConnectivityTransition None = new(SyncTransitionKind.None, null, null);
}
