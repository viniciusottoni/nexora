using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Convite de posse/administração enviado a um e-mail, consumível uma única vez. Estendido pela
/// US-155 (Proprietários, usuários iniciais e convites) com o ciclo de vida ADMINISTRATIVO que
/// faltava sobre o convite já existente (criado no provisionamento, US-002): reenvio/correção
/// (<see cref="Reason"/>, um novo <see cref="OwnerInvite"/> é sempre criado para isso — ver
/// <see cref="Revoke"/> abaixo) e revogação (<see cref="Revoke"/>). O segredo em si nunca é
/// reexposto: reenviar/corrigir sempre RESULTA num novo <see cref="OwnerInvite"/> com novo
/// <see cref="SecretHash"/>, nunca em mutar o hash de um convite existente.
/// </summary>
public sealed class OwnerInvite
{
    private OwnerInvite() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string SecretHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// US-155 · Preenchido quando este convite foi REVOGADO administrativamente (reenvio,
    /// correção de e-mail ou revogação explícita) — nulo enquanto pendente ou depois de aceito.
    /// Convite consumido nunca pode ser revogado (<see cref="Revoke"/> lança <see cref="DomainException"/>):
    /// não há nada a invalidar num convite que já cumpriu seu propósito.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Motivo substantivo da revogação (RN-004) — obrigatório sempre que <see cref="RevokedAt"/> é preenchido.</summary>
    public string? RevokedReason { get; private set; }

    /// <summary>
    /// Motivo da CRIAÇÃO deste convite específico quando ele nasce de um reenvio/correção
    /// administrativa (<c>ReissueOwnerInviteCommand</c>) — nulo para o convite original emitido
    /// pelo provisionamento (<c>ProvisionTenantCommandHandler</c>), que não tem "motivo": é o
    /// primeiro convite do tenant, não uma correção de nada.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Referência ao <see cref="EmailOutbox"/> criado junto com este convite (US-155) — permite ao
    /// histórico administrativo (<c>GetTenantOwnershipQueryHandler</c>) reportar o status de ENTREGA
    /// (pendente/enviado/falho) sem duplicar o ciclo de vida do outbox dentro do convite. Nulo para
    /// convites emitidos antes desta US (provisionamento original) — o histórico reporta esses como
    /// entrega "UNKNOWN", não como falha.
    /// </summary>
    public Guid? EmailOutboxId { get; private set; }

    public AppUser User { get; private set; } = null!;

    public static OwnerInvite Create(
        Guid tenantId,
        Guid userId,
        string email,
        string secretHash,
        DateTimeOffset expiresAt,
        string? reason = null,
        Guid? emailOutboxId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail do convite é obrigatório.");

        if (string.IsNullOrWhiteSpace(secretHash))
            throw new DomainException("O segredo do convite é obrigatório.");

        return new OwnerInvite
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            UserId = userId,
            Email = email,
            SecretHash = secretHash,
            ExpiresAt = expiresAt,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            EmailOutboxId = emailOutboxId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsConsumed => ConsumedAt is not null;

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// <c>PENDING</c> (ainda pode ser aceito) · <c>ACCEPTED</c> · <c>EXPIRED</c> · <c>REVOKED</c> —
    /// consumido tem prioridade sobre revogado/expirado (um convite aceito não "desrevoga" ao ser
    /// consultado depois, mas na prática <see cref="Revoke"/> já proíbe revogar convite consumido,
    /// então os dois nunca coexistem de verdade; a ordem aqui só documenta a prioridade).
    /// </summary>
    public string ResolveStatus(DateTimeOffset now)
    {
        if (IsConsumed) return "ACCEPTED";
        if (IsRevoked) return "REVOKED";
        if (IsExpired(now)) return "EXPIRED";
        return "PENDING";
    }

    public void Consume()
    {
        if (IsConsumed)
            throw new DomainException("Este convite já foi utilizado.");

        ConsumedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// US-155 · Invalida este convite (reenvio, correção de e-mail ou revogação explícita) — o
    /// segredo bruto já enviado ao destinatário original deixa de servir: <c>AcceptOwnerInvitation</c>
    /// não distingue revogado de expirado/inexistente na resposta (mesma decisão de não vazar motivo
    /// específico, ver <c>ApiErrorCodes.OwnerInviteInvalidCredentials</c>), mas aqui, no lado
    /// administrativo, o motivo fica registrado para auditoria.
    /// </summary>
    public void Revoke(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo da revogação é obrigatório.");

        if (IsConsumed)
            throw new DomainException("Este convite já foi aceito e não pode ser revogado.");

        if (IsRevoked)
            throw new DomainException("Este convite já foi revogado.");

        RevokedAt = DateTimeOffset.UtcNow;
        RevokedReason = reason.Trim();
    }
}
