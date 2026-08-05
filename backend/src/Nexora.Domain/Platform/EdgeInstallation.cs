using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Instalação do servidor edge (mini-PC) de uma loja — autoridade operacional local
/// de pedido, mesa, comanda, KDS e caixa. Uma instalação por loja (uq_edge_store).
/// </summary>
public sealed class EdgeInstallation
{
    private EdgeInstallation() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string? PublicKey { get; private set; }
    public string? Version { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public long LastSyncedSeq { get; private set; }
    public int? ClockOffsetMs { get; private set; }

    // TODO: value object tipado quando o formato de health for definido — hoje é JSONB livre
    public string Health { get; private set; } = "{}";

    /// <summary>Último estado de conectividade edge↔nuvem observado (US-034 §6) — ver <see cref="RecordHeartbeat"/>.</summary>
    public SyncConnectivity Connectivity { get; private set; } = SyncConnectivity.Unknown;

    /// <summary>
    /// Instante em que a nuvem foi observada inalcançável pela última vez — não nulo enquanto
    /// <see cref="Connectivity"/> for <see cref="SyncConnectivity.Offline"/>; volta a nulo no
    /// exato heartbeat que detecta a reconexão (US-034 §7, campo <c>offlineSince</c> do
    /// <c>GET /v1/health</c>).
    /// </summary>
    public DateTimeOffset? OfflineSince { get; private set; }

    public string? InstallTokenHash { get; private set; }
    public DateTimeOffset? TokenExpiresAt { get; private set; }
    public DateTimeOffset? TokenConsumedAt { get; private set; }
    public DateTimeOffset? InstalledAt { get; private set; }

    /// <summary>Versão que a liberação gradual da release corrente atribuiu a esta instalação (US-146) — nulo quando já está em dia.</summary>
    public string? TargetVersion { get; private set; }

    /// <summary>Instante da última tentativa de atualização (sucesso, falha ou rollback) — US-146.</summary>
    public DateTimeOffset? LastUpdateAt { get; private set; }

    /// <summary>Resultado da última tentativa de atualização (<see cref="EdgeUpdateStatus"/> serializado) — US-146.</summary>
    public string? LastUpdateStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Store Store { get; private set; } = null!;

    /// <summary>Instalação já concluiu o pareamento (chave pública recebida e aceita).</summary>
    public bool IsInstalled => InstalledAt is not null;

    /// <summary>Token de instalação já foi reservado/consumido (ver <see cref="ReserveInstallToken"/>).</summary>
    public bool IsTokenConsumed => TokenConsumedAt is not null;

    /// <summary>
    /// US-156 · Recuperação do provisionamento — verdadeiro enquanto a instalação ainda não
    /// concluiu o pareamento (<see cref="IsInstalled"/> falso). Um administrador de plataforma só
    /// pode reemitir/rotacionar o token de instalação nessa janela: uma vez pareada, a via correta
    /// deixa de ser "recuperar o token perdido" e passa a ser o fluxo de manutenção do parque já
    /// instalado (US-146) — reemitir um token para uma instalação já registrada não tem efeito
    /// útil (o dispositivo já trocou de chave pública e nunca mais consultará este token).
    /// </summary>
    public bool CanReissueToken => !IsInstalled;

    public static EdgeInstallation Create(Guid tenantId, Guid storeId, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("O rótulo da instalação edge é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new EdgeInstallation
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            Label = label,
            LastSyncedSeq = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Reconstrução direta do lado edge durante a importação do bootstrap (ADR-019 fluxo de
    /// primeira subida) — o container local já recebeu seu próprio id (INSTALLATION_ID) e a
    /// chave pública gerada localmente, sem passar pelo protocolo de token da nuvem.
    /// Espelha o branch "create" de <c>persistBootstrap</c> (import-bootstrap.ts).
    /// </summary>
    public static EdgeInstallation CreateInstalled(
        Guid id,
        Guid tenantId,
        Guid storeId,
        string label,
        string publicKey,
        string version)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new DomainException("A chave pública da instalação edge é obrigatória.");

        var now = DateTimeOffset.UtcNow;

        return new EdgeInstallation
        {
            Id = id,
            TenantId = tenantId,
            StoreId = storeId,
            Label = string.IsNullOrWhiteSpace(label) ? "Edge local" : label,
            PublicKey = publicKey,
            Version = version,
            LastSyncedSeq = 0,
            InstalledAt = now,
            TokenConsumedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void IssueInstallToken(string installTokenHash, DateTimeOffset tokenExpiresAt)
    {
        InstallTokenHash = installTokenHash;
        TokenExpiresAt = tokenExpiresAt;
        TokenConsumedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsTokenExpired(DateTimeOffset now) => TokenExpiresAt is null || now >= TokenExpiresAt;

    /// <summary>
    /// Reserva o token de uso único sem concluir o pareamento — usado pelo passo de
    /// "consumo do token" que precede o registro físico do edge (a chave pública só chega
    /// depois, quando o dispositivo efetivamente sobe — ver <see cref="CompleteRegistration"/>).
    /// Idempotência/replay do token em si é responsabilidade do handler (ADR-020 aplica-se à
    /// escrita HTTP; aqui é reforçado pela checagem de <see cref="IsTokenConsumed"/>).
    /// </summary>
    public void ReserveInstallToken()
    {
        TokenConsumedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// US-156 · Revogação manual de credencial comprometida — invalida o token "corrente" (campos
    /// legados <see cref="InstallTokenHash"/>/<see cref="TokenExpiresAt"/>) sem fingir que ele foi
    /// de fato consumido por um registro real do edge. Usado só quando a credencial revogada pelo
    /// administrador (<c>RevokeInstallationCredentialCommandHandler</c>) É a que os campos legados
    /// ainda apontam (a rotação mais recente) — reaproveita o mesmo sinalizador que
    /// <see cref="IsTokenConsumed"/> já expõe, porque <c>ConsumeInstallationTokenCommandHandler</c>/
    /// <c>RegisterInstallationCommandHandler</c> só entendem "consumido ou não", sem um terceiro
    /// estado "revogado" — o motivo real (revogação manual, não um pareamento de verdade) fica
    /// registrado no <c>AuditLog</c> correspondente, nunca aqui no Domain. Idempotente.
    /// </summary>
    public void InvalidateInstallToken()
    {
        if (TokenConsumedAt is null)
        {
            TokenConsumedAt = DateTimeOffset.UtcNow;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkInstalled(string publicKey)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new DomainException("A chave pública da instalação edge é obrigatória.");

        PublicKey = publicKey;
        InstalledAt = DateTimeOffset.UtcNow;
        TokenConsumedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Conclui o registro iniciado pelo protocolo de token da nuvem (POST .../installations/register
    /// — RegisterInstallationCommand), gravando também rótulo/versão informados pelo dispositivo.
    /// Idempotente: reenviar a mesma chave pública não é erro (ver RegisterInstallationCommandHandler).
    /// </summary>
    public void CompleteRegistration(string publicKey, string? version, string? label)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new DomainException("A chave pública da instalação edge é obrigatória.");

        PublicKey = publicKey;

        if (!string.IsNullOrWhiteSpace(version))
            Version = version;

        if (!string.IsNullOrWhiteSpace(label))
            Label = label;

        InstalledAt = DateTimeOffset.UtcNow;
        TokenConsumedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Registra o resultado de um ping de saúde (US-006, <c>PollSyncHealthCommand</c>) e decide,
    /// no próprio Domain, se isso representa uma TRANSIÇÃO de conectividade (US-034 §6: "o edge não
    /// reimplementa sincronização — só detecta e comunica"). <paramref name="observedConnectivity"/>
    /// já vem traduzido pelo chamador (Application) a partir do resultado bruto do poller
    /// (<c>DependencyStatus</c>) — o Domain nunca conhece esse tipo, só o vocabulário próprio
    /// (<see cref="SyncConnectivity"/>), preservando a regra de "Domain sem dependência de
    /// Infrastructure/Application" (ADR-039).
    ///
    /// <see cref="SyncConnectivity.Unknown"/> nunca dispara transição (nem grava
    /// <see cref="OfflineSince"/>) — é o caso de ambiente sem <c>SyncEndpoint</c> configurado
    /// (dev/teste) ou o primeiro poll antes de qualquer sinal real: sem observação real de
    /// conectividade, não há "queda" nem "retorno" para anunciar.
    /// </summary>
    public SyncConnectivityTransition RecordHeartbeat(
        long syncedSeq, int? clockOffsetMs, SyncConnectivity observedConnectivity, string? healthJson = null)
    {
        var now = DateTimeOffset.UtcNow;

        LastSeenAt = now;
        LastSyncedSeq = syncedSeq;
        ClockOffsetMs = clockOffsetMs;

        if (healthJson is not null)
            Health = healthJson;

        var transition = SyncConnectivityTransition.None;

        if (observedConnectivity != SyncConnectivity.Unknown && observedConnectivity != Connectivity)
        {
            if (observedConnectivity == SyncConnectivity.Offline)
            {
                // EVT-083 edge.offline_detected — Online/Unknown -> Offline.
                OfflineSince = now;
                transition = new SyncConnectivityTransition(SyncTransitionKind.WentOffline, now, null);
            }
            else if (Connectivity == SyncConnectivity.Offline)
            {
                // EVT-084 edge.reconnected — só é "reconexão" quando o estado ANTERIOR era
                // Offline de verdade (Unknown -> Online, na primeira observação, não conta —
                // não houve queda para retornar de).
                var offlineSince = OfflineSince;
                var duration = offlineSince is null ? (TimeSpan?)null : now - offlineSince.Value;
                transition = new SyncConnectivityTransition(SyncTransitionKind.Reconnected, offlineSince, duration);
                OfflineSince = null;
            }

            Connectivity = observedConnectivity;
        }

        UpdatedAt = now;
        return transition;
    }

    /// <summary>Atribui uma versão-alvo à instalação (US-146, avaliação de elegibilidade de rollout de <c>Release</c>).</summary>
    public void ScheduleUpdate(string targetVersion)
    {
        if (string.IsNullOrWhiteSpace(targetVersion))
            throw new DomainException("A versão-alvo da atualização é obrigatória.");

        TargetVersion = targetVersion;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Registra o resultado do ciclo de atualização (download → migration → health check → ativa
    /// ou reverte, US-146 §7). Em <see cref="EdgeUpdateStatus.Succeeded"/>, <see cref="Version"/>
    /// passa a refletir a versão instalada e <see cref="TargetVersion"/> é limpo (já em dia).
    /// </summary>
    public void RecordUpdateResult(EdgeUpdateStatus status, DateTimeOffset at, string? installedVersion = null)
    {
        LastUpdateStatus = status.ToString();
        LastUpdateAt = at;

        if (status == EdgeUpdateStatus.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(installedVersion))
                Version = installedVersion;

            TargetVersion = null;
        }

        UpdatedAt = at;
    }
}

/// <summary>Resultado de um ciclo de atualização controlada do parque (US-146).</summary>
public enum EdgeUpdateStatus
{
    Deferred = 0,
    InProgress = 1,
    Succeeded = 2,
    Failed = 3,
    RolledBack = 4
}
