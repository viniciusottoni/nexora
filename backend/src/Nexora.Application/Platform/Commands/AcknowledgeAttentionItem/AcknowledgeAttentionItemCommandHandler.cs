using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Support;
using Nexora.Application.Platform.Support;
using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Platform.Commands.AcknowledgeAttentionItem;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — Gherkin "Reconhecimento/resolução
/// de pendência administrativa SEM apagar o fato original". <see cref="AttentionItemId.TryDecode"/>
/// já embute o tenant na chave opaca (ver docstring dela) — não precisa de outra consulta prévia
/// para descobrir a que tenant o item pertence antes de fixar o contexto RLS.
/// </summary>
internal sealed class AcknowledgeAttentionItemCommandHandler
    : IRequestHandler<AcknowledgeAttentionItemCommand, Result<AttentionAcknowledgementResponse>>
{
    private readonly IApplicationDbContext _db;

    public AcknowledgeAttentionItemCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AttentionAcknowledgementResponse>> Handle(
        AcknowledgeAttentionItemCommand request, CancellationToken cancellationToken)
    {
        var decoded = AttentionItemId.TryDecode(request.ItemId);
        if (decoded is null)
        {
            return Result<AttentionAcknowledgementResponse>.Failure(
                "Este item da fila de atenção não foi encontrado.", ApiErrorCodes.AttentionItemNotFound);
        }

        var tenantExists = await _db.Tenants.AsNoTracking()
            .AnyAsync(t => t.Id == decoded.TenantId && t.DeletedAt == null, cancellationToken);

        if (!tenantExists)
        {
            return Result<AttentionAcknowledgementResponse>.Failure(
                "Este item da fila de atenção não foi encontrado.", ApiErrorCodes.AttentionItemNotFound);
        }

        await _db.SetTenantContextAsync(decoded.TenantId, cancellationToken);

        if (!await IsActiveAttentionItemAsync(decoded, cancellationToken))
        {
            return Result<AttentionAcknowledgementResponse>.Failure(
                "Este item da fila de atenção não foi encontrado.", ApiErrorCodes.AttentionItemNotFound);
        }

        var acknowledgement = AdministrativeAttentionAcknowledgement.Create(
            decoded.TenantId, request.ItemId, decoded.Type.ToWireLabel(), request.Reason, request.ActorId);

        _db.AdministrativeAttentionAcknowledgements.Add(acknowledgement);

        return Result<AttentionAcknowledgementResponse>.Success(new AttentionAcknowledgementResponse(
            acknowledgement.Id, acknowledgement.ItemId, acknowledgement.Reason, acknowledgement.AcknowledgedAt));
    }

    private async Task<bool> IsActiveAttentionItemAsync(
        AttentionItemIdValue item,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        if (item.Type is AttentionItemType.InstallationOffline or AttentionItemType.InstallationDegraded)
        {
            var installation = await _db.EdgeInstallations.AsNoTracking()
                .Where(i => i.Id == item.SourceId && i.TenantId == item.TenantId && i.InstalledAt != null)
                .Select(i => new { i.LastSeenAt })
                .FirstOrDefaultAsync(cancellationToken);

            if (installation is null)
                return false;

            var health = InstallationHealthClassifier.Classify(now, installation.LastSeenAt);
            return item.Type == AttentionItemType.InstallationOffline
                ? health == InstallationHealthStatus.Down
                : health == InstallationHealthStatus.Degraded;
        }

        if (item.Type == AttentionItemType.InviteExpired)
        {
            return await _db.OwnerInvites.AsNoTracking().AnyAsync(
                i => i.Id == item.SourceId
                    && i.TenantId == item.TenantId
                    && i.ConsumedAt == null
                    && i.RevokedAt == null
                    && i.ExpiresAt <= now,
                cancellationToken);
        }

        if (item.Type != AttentionItemType.ProvisioningStalled || item.SourceId != item.TenantId)
            return false;

        var tenant = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == item.TenantId
                && t.DeletedAt == null
                && (t.Status == TenantStatus.Provisioned || t.Status == TenantStatus.Installing))
            .Select(t => new { t.Status, t.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
            return false;

        var latestTransitionInto = await _db.TenantStatusHistories.AsNoTracking()
            .Where(h => h.TenantId == item.TenantId && h.NewStatus == tenant.Status)
            .OrderByDescending(h => h.EffectiveAt)
            .Select(h => (DateTimeOffset?)h.EffectiveAt)
            .FirstOrDefaultAsync(cancellationToken);

        var since = latestTransitionInto ?? tenant.CreatedAt;
        return AttentionQueueClassifier.ClassifyProvisioningStalled(now - since) is not null;
    }
}
