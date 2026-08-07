using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — histórico ROTACIONÁVEL de
/// credenciais de instalação de um <see cref="EdgeInstallation"/>, uma linha por emissão. Nasce
/// para fechar a lacuna do modelo anterior: <see cref="EdgeInstallation.InstallTokenHash"/> só
/// guardava UM token por instalação, sem histórico de reemissões nem revogação explícita — se a
/// resposta do <c>201</c> do provisionamento se perdesse antes de o token ser copiado, não havia
/// como recuperar com segurança sem recriar tenant/loja/instalação.
/// </summary>
/// <remarks>
/// <para>
/// <b>Convivência com os campos legados (decisão de design, ver relatório da US-156):</b> esta
/// entidade NÃO substitui <see cref="EdgeInstallation.InstallTokenHash"/>/
/// <see cref="EdgeInstallation.TokenExpiresAt"/>/<see cref="EdgeInstallation.TokenConsumedAt"/> —
/// convive com eles por DELEGAÇÃO. A cada reemissão, o handler de aplicação
/// (<c>ReissueInstallationTokenCommandHandler</c>) cria uma linha nova aqui E chama
/// <see cref="EdgeInstallation.IssueInstallToken"/> (método já existente, sem mudar assinatura) para
/// que os campos legados sempre reflitam o token MAIS RECENTE. Isso preserva, sem qualquer
/// alteração, o protocolo público que <c>ConsumeInstallationTokenCommandHandler</c> e
/// <c>RegisterInstallationCommandHandler</c> já usam (ambos continuam comparando só contra
/// <see cref="EdgeInstallation.InstallTokenHash"/>) — o dispositivo edge nunca precisa saber que a
/// nuvem agora guarda histórico. Esta tabela existe só para auditoria/histórico/rotação
/// administrativa (nunca é lida pelo protocolo de pareamento do edge).
/// </para>
/// <para>
/// <b>Segredo nunca em texto puro (RN-004):</b> <see cref="TokenHash"/> é o único vestígio do
/// segredo — o mesmo algoritmo de digest já usado no provisionamento original
/// (<c>Nexora.Application.Installations.Abstractions.IInstallationTokenDigester</c>, SHA-256 puro,
/// a MESMA função que <c>ConsumeInstallationTokenCommandHandler</c>/<c>RegisterInstallationCommandHandler</c>
/// usam para validar — ver justificativa completa no relatório da tarefa sobre por que NÃO se usa
/// <c>ISecretDigester</c>/<c>HmacSecretDigester</c> aqui, apesar de ser a técnica do
/// <c>ProvisionTenantCommandHandler</c> original).
/// </para>
/// </remarks>
public sealed class InstallationCredential
{
    private InstallationCredential() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid InstallationId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Motivo da EMISSÃO (ex.: "Comando original não foi exibido") — o motivo de uma eventual revogação vive só no <c>AuditLog</c> correspondente, nunca aqui.</summary>
    public string? Reason { get; private set; }

    /// <summary>Administrador de plataforma que emitiu esta credencial (RN-004 "ator") — nulo só em cenário de sistema/migração.</summary>
    public Guid? ActorId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>Ainda pode ser consumida pelo protocolo de pareamento: não consumida, não revogada e dentro da validade.</summary>
    public bool IsActive(DateTimeOffset now) => ConsumedAt is null && RevokedAt is null && !IsExpired(now);

    public static InstallationCredential Issue(
        Guid tenantId,
        Guid installationId,
        string tokenHash,
        DateTimeOffset expiresAt,
        string reason,
        Guid? actorId)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("O hash do token de instalação é obrigatório.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo da emissão da credencial é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new InstallationCredential
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            InstallationId = installationId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            Reason = reason,
            ActorId = actorId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Marca como consumida pelo protocolo real de pareamento (uso futuro — hoje o consumo de fato acontece via os campos legados de <see cref="EdgeInstallation"/>, ver docstring da classe). Idempotente.</summary>
    public void MarkConsumed(DateTimeOffset at)
    {
        if (ConsumedAt is not null)
            return;

        ConsumedAt = at;
        UpdatedAt = at;
    }

    /// <summary>Revogação — manual (credencial comprometida) ou automática (substituída por uma reemissão). Idempotente: revogar de novo não é erro.</summary>
    public void Revoke(DateTimeOffset at)
    {
        if (RevokedAt is not null)
            return;

        RevokedAt = at;
        UpdatedAt = at;
    }
}
