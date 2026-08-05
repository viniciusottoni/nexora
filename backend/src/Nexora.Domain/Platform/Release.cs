using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Versão publicada do software de edge (US-146) — catálogo de plataforma, sem <c>tenant_id</c>
/// (mesma natureza de <see cref="Provisioning.ProvisioningTemplate"/>/<c>business_template</c>:
/// dado da Replay, não de um estabelecimento). Atualização é PUXADA pelo edge (doc. 02 §9.3), nunca
/// empurrada — este registro só declara o que está disponível e para qual fatia do parque
/// (<see cref="RolloutPercent"/>), a decisão de aplicar é de cada <see cref="EdgeInstallation"/>.
/// </summary>
public sealed class Release
{
    private Release() { }

    public Guid Id { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public int RolloutPercent { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public Guid? PublishedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Release Publish(string version, int rolloutPercent, string? notes, Guid? publishedBy)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new DomainException("A versão da release é obrigatória.");

        if (rolloutPercent is < 0 or > 100)
            throw new DomainException("O percentual de liberação deve estar entre 0 e 100.");

        var now = DateTimeOffset.UtcNow;

        return new Release
        {
            Id = IdGenerator.NewId(),
            Version = version,
            RolloutPercent = rolloutPercent,
            Notes = notes,
            PublishedAt = now,
            PublishedBy = publishedBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Amplia a liberação gradual — nunca reduz (US-146 §3.1 "liberação gradual").</summary>
    public void ExpandRollout(int rolloutPercent)
    {
        if (rolloutPercent is < 0 or > 100)
            throw new DomainException("O percentual de liberação deve estar entre 0 e 100.");

        if (rolloutPercent <= RolloutPercent)
            return;

        RolloutPercent = rolloutPercent;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Decide, de forma determinística e estável por instalação, se ela está no subconjunto
    /// liberado (US-146, cenário "Liberação gradual": "deve atingir um subconjunto primeiro"). O
    /// mesmo par (release, instalação) sempre cai no mesmo balde — evita instalações "piscando"
    /// dentro/fora do rollout a cada nova checagem antes do percentual mudar.
    /// </summary>
    public bool IsEligibleFor(Guid installationId)
    {
        if (RolloutPercent >= 100)
            return true;

        if (RolloutPercent <= 0)
            return false;

        var bucket = Math.Abs(HashCode.Combine(Id, installationId)) % 100;
        return bucket < RolloutPercent;
    }
}
