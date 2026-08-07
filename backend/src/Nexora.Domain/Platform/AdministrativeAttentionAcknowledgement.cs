using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — reconhecimento de uma pendência
/// da fila de atenção da plataforma (<c>GetAttentionQueueQuery</c>), SEM apagar o fato original que
/// a gerou (RN-004, "linha do tempo é append-only, preserva autor/contexto"): a instalação offline,
/// o convite expirado ou o provisionamento parado continuam intactos em suas próprias tabelas — esta
/// entidade é só um registro PRÓPRIO de "um administrador viu isto e decidiu não agir agora",
/// append-only (sem <c>Resolve</c>/<c>Revoke</c>, mesmo espírito de <see cref="AuditLog"/>/
/// <see cref="DomainEvent"/>: nasce completa, nunca muda depois).
/// </summary>
/// <remarks>
/// Única tabela nova desta US — todo o resto da central de atenção é projeção de leitura sobre
/// tabelas já existentes (<c>edge_installation</c>, <c>owner_invite</c>, <c>tenant</c>,
/// <c>tenant_status_history</c>, <c>tenant_plan_history</c>, <c>ownership_transfer</c>,
/// <c>installation_credential</c>, <c>tenant_domain</c>, <c>support_access</c>,
/// <c>installation_incident</c>). <see cref="ItemId"/> é a mesma chave opaca
/// <c>{TIPO}|{tenantId}|{sourceId}</c> exposta como <c>id</c> pelo item da fila
/// (<c>Nexora.Application.Platform.Support.AttentionItemId</c>) — permite ao handler de leitura
/// verificar, sem precisar de outra tabela de junção, se a MESMA condição (mesmo <see cref="ItemId"/>)
/// já foi reconhecida depois do instante em que ela começou (<c>since</c> do item); se a condição se
/// repetir depois (ex.: instalação volta a ficar offline após reconectar), o novo <c>since</c> é
/// posterior ao reconhecimento antigo e o item reaparece na fila — nenhum reconhecimento "silencia"
/// permanentemente uma pendência.
/// </remarks>
public sealed class AdministrativeAttentionAcknowledgement
{
    private AdministrativeAttentionAcknowledgement() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Chave opaca do item reconhecido — <c>{TIPO}|{tenantId}|{sourceId}</c>, ver <see cref="Nexora.Domain.Platform"/> docstring acima.</summary>
    public string ItemId { get; private set; } = string.Empty;

    /// <summary>Tipo do item no instante do reconhecimento (rótulo estável, ex. <c>INSTALLATION_OFFLINE</c>) — guardado por conveniência de auditoria/export, mesmo já implícito em <see cref="ItemId"/>.</summary>
    public string ItemType { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;
    public Guid? ActorId { get; private set; }
    public DateTimeOffset AcknowledgedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    public static AdministrativeAttentionAcknowledgement Create(
        Guid tenantId,
        string itemId,
        string itemType,
        string reason,
        Guid? actorId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new DomainException("O item reconhecido é obrigatório.");

        if (string.IsNullOrWhiteSpace(itemType))
            throw new DomainException("O tipo do item reconhecido é obrigatório.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo do reconhecimento é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new AdministrativeAttentionAcknowledgement
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            ItemId = itemId,
            ItemType = itemType,
            Reason = reason.Trim(),
            ActorId = actorId,
            AcknowledgedAt = now,
            CreatedAt = now
        };
    }
}
