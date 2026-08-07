using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Support;
using Nexora.Application.Platform.Support;
using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Platform.Queries.GetAttentionQueue;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — Gherkin "Priorização explicável"
/// e "Falha parcial". A fila NÃO tem tabela própria: é uma projeção agregada de três fontes
/// independentes (saúde de instalação — US-140, convites pendentes — US-155, ciclo de vida do
/// provisionamento — US-153), cada uma buscada tenant a tenant (RLS, mesmo padrão de
/// <c>GetPlatformSummaryQueryHandler</c>/<c>ListTenantsQueryHandler</c>) e isolada por
/// <see cref="PartialFailureAggregator"/>: uma fonte falhando marca só a SI MESMA em
/// <c>meta.unavailableSources</c> — as outras duas continuam aparecendo normalmente. A fila inteira é
/// materializada em memória (número de tenants ativos é o limitante, não um índice dedicado — mesma
/// ressalva de escala já documentada em <c>ListTenantsQueryHandler</c>) e paginada por cursor depois
/// de ordenada por criticidade.
/// </summary>
internal sealed class GetAttentionQueueQueryHandler : IRequestHandler<GetAttentionQueueQuery, Result<AttentionQueueListResponse>>
{
    private const string InstallationHealthSource = "INSTALLATION_HEALTH";
    private const string OwnerInvitesSource = "OWNER_INVITES";
    private const string ProvisioningLifecycleSource = "PROVISIONING_LIFECYCLE";

    private readonly IApplicationDbContext _db;

    public GetAttentionQueueQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AttentionQueueListResponse>> Handle(GetAttentionQueueQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var tenants = await _db.Tenants.AsNoTracking()
            .Where(t => t.DeletedAt == null && t.Status != TenantStatus.Cancelled)
            .Select(t => new TenantSlice(t.Id, t.Name, t.Status, t.CreatedAt))
            .ToListAsync(cancellationToken);

        var sources = new List<AttentionSource<RawAttentionItem>>
        {
            new(InstallationHealthSource, ct => FetchInstallationHealthItemsAsync(tenants, now, ct)),
            new(OwnerInvitesSource, ct => FetchExpiredInviteItemsAsync(tenants, now, ct)),
            new(ProvisioningLifecycleSource, ct => FetchStalledProvisioningItemsAsync(tenants, now, ct)),
        };

        var collected = await PartialFailureAggregator.CollectAsync(sources, cancellationToken);

        var severityFilter = request.Severity.Count == 0 ? null : new HashSet<AttentionSeverity>(request.Severity);

        var ordered = collected.Items
            .Where(item => severityFilter is null || severityFilter.Contains(item.Severity))
            .OrderBy(item => item.Severity.RankOf())
            .ThenBy(item => item.Since)
            .ThenBy(item => item.ItemId, StringComparer.Ordinal)
            .ToList();

        var cursorValue = AttentionQueueCursor.Decode(request.Cursor);
        var afterCursor = cursorValue is null
            ? ordered
            : ordered.Where(item => IsAfterCursor(item, cursorValue)).ToList();

        var page = afterCursor.Take(request.Limit + 1).ToList();
        var hasMore = page.Count > request.Limit;
        if (hasMore)
            page = page.Take(request.Limit).ToList();

        var data = page.Select(ToResponse).ToList();
        var nextCursor = hasMore && page.Count > 0
            ? AttentionQueueCursor.Encode(page[^1].Severity.RankOf(), page[^1].Since, page[^1].ItemId)
            : null;

        var meta = new AttentionQueueMetaResponse(now, collected.UnavailableSources);
        return Result<AttentionQueueListResponse>.Success(new AttentionQueueListResponse(data, nextCursor, meta));
    }

    private static bool IsAfterCursor(RawAttentionItem item, AttentionQueueCursorValue cursor)
    {
        var rank = item.Severity.RankOf();
        if (rank != cursor.SeverityRank)
            return rank > cursor.SeverityRank;

        if (item.Since != cursor.Since)
            return item.Since > cursor.Since;

        return string.CompareOrdinal(item.ItemId, cursor.ItemId) > 0;
    }

    private static AttentionQueueItemResponse ToResponse(RawAttentionItem item)
    {
        var action = item.Type is AttentionItemType.InstallationOffline or AttentionItemType.InstallationDegraded
            ? new AttentionActionResponse("OPEN_DIAGNOSTICS", "/instalacoes")
            : new AttentionActionResponse("OPEN_TENANT", $"/estabelecimentos/{item.TenantId}");

        return new AttentionQueueItemResponse(
            item.ItemId,
            item.TenantId,
            item.TenantName,
            item.Type.ToWireLabel(),
            item.Severity.ToWireLabel(),
            item.Since,
            item.Reason,
            action);
    }

    /// <summary>US-140 (saúde de instalação) — OFFLINE (DOWN) e DEGRADED, ambos a partir da mesma defasagem de <c>LastSeenAt</c> já usada pelo painel de instalações.</summary>
    private async Task<IReadOnlyList<RawAttentionItem>> FetchInstallationHealthItemsAsync(
        IReadOnlyList<TenantSlice> tenants, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var items = new List<RawAttentionItem>();

        foreach (var tenant in tenants)
        {
            await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

            var installations = await _db.EdgeInstallations.AsNoTracking()
                .Where(i => i.TenantId == tenant.Id && i.InstalledAt != null)
                .Select(i => new { i.Id, i.LastSeenAt, i.CreatedAt })
                .ToListAsync(cancellationToken);

            foreach (var installation in installations)
            {
                var health = InstallationHealthClassifier.Classify(now, installation.LastSeenAt);
                if (health == InstallationHealthStatus.Ok)
                    continue;

                var since = installation.LastSeenAt ?? installation.CreatedAt;
                var elapsed = now - since;

                var type = health == InstallationHealthStatus.Down
                    ? AttentionItemType.InstallationOffline
                    : AttentionItemType.InstallationDegraded;

                var severity = type == AttentionItemType.InstallationOffline
                    ? AttentionQueueClassifier.ClassifyInstallationOffline(elapsed)
                    : AttentionQueueClassifier.ClassifyInstallationDegraded();

                var reason = type == AttentionItemType.InstallationOffline
                    ? AttentionQueueClassifier.ReasonForInstallationOffline(elapsed)
                    : AttentionQueueClassifier.ReasonForInstallationDegraded(elapsed);

                var itemId = AttentionItemId.Encode(type, tenant.Id, installation.Id);
                if (await IsAcknowledgedAsync(tenant.Id, itemId, since, cancellationToken))
                    continue;

                items.Add(new RawAttentionItem(itemId, type, tenant.Id, tenant.Name, severity, since, reason));
            }
        }

        return items;
    }

    /// <summary>US-155 (convites) — convite pendente cuja validade já expirou (nunca aceito, nunca revogado).</summary>
    private async Task<IReadOnlyList<RawAttentionItem>> FetchExpiredInviteItemsAsync(
        IReadOnlyList<TenantSlice> tenants, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var items = new List<RawAttentionItem>();

        foreach (var tenant in tenants)
        {
            await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

            var expiredInvites = await _db.OwnerInvites.AsNoTracking()
                .Where(i => i.TenantId == tenant.Id && i.ConsumedAt == null && i.RevokedAt == null && i.ExpiresAt <= now)
                .Select(i => new { i.Id, i.ExpiresAt })
                .ToListAsync(cancellationToken);

            foreach (var invite in expiredInvites)
            {
                var elapsed = now - invite.ExpiresAt;
                var severity = AttentionQueueClassifier.ClassifyInviteExpired(elapsed);
                var reason = AttentionQueueClassifier.ReasonForInviteExpired(elapsed);

                var itemId = AttentionItemId.Encode(AttentionItemType.InviteExpired, tenant.Id, invite.Id);
                if (await IsAcknowledgedAsync(tenant.Id, itemId, invite.ExpiresAt, cancellationToken))
                    continue;

                items.Add(new RawAttentionItem(itemId, AttentionItemType.InviteExpired, tenant.Id, tenant.Name, severity, invite.ExpiresAt, reason));
            }
        }

        return items;
    }

    /// <summary>US-153 (ciclo de vida) — tenant em PROVISIONED/INSTALLING há mais tempo que o normal do roteiro de implantação.</summary>
    private async Task<IReadOnlyList<RawAttentionItem>> FetchStalledProvisioningItemsAsync(
        IReadOnlyList<TenantSlice> tenants, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var items = new List<RawAttentionItem>();

        foreach (var tenant in tenants)
        {
            if (tenant.Status != TenantStatus.Provisioned && tenant.Status != TenantStatus.Installing)
                continue;

            await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

            var latestTransitionInto = await _db.TenantStatusHistories.AsNoTracking()
                .Where(h => h.TenantId == tenant.Id && h.NewStatus == tenant.Status)
                .OrderByDescending(h => h.EffectiveAt)
                .Select(h => (DateTimeOffset?)h.EffectiveAt)
                .FirstOrDefaultAsync(cancellationToken);

            var since = latestTransitionInto ?? tenant.CreatedAt;
            var elapsed = now - since;
            var severity = AttentionQueueClassifier.ClassifyProvisioningStalled(elapsed);
            if (severity is null)
                continue;

            var statusLabel = tenant.Status == TenantStatus.Provisioned ? "PROVISIONED" : "INSTALLING";
            var reason = AttentionQueueClassifier.ReasonForProvisioningStalled(statusLabel, elapsed);

            var itemId = AttentionItemId.Encode(AttentionItemType.ProvisioningStalled, tenant.Id, tenant.Id);
            if (await IsAcknowledgedAsync(tenant.Id, itemId, since, cancellationToken))
                continue;

            items.Add(new RawAttentionItem(itemId, AttentionItemType.ProvisioningStalled, tenant.Id, tenant.Name, severity.Value, since, reason));
        }

        return items;
    }

    /// <summary>
    /// Gherkin "Reconhecimento/resolução... SEM apagar o fato original" — um reconhecimento só
    /// suprime o item enquanto a MESMA ocorrência persiste: se a condição já era conhecida quando o
    /// administrador reconheceu (<c>AcknowledgedAt &gt;= since</c>), o item fica oculto; se a condição
    /// se repetiu depois (nova instalação offline após reconectar, por exemplo), <c>since</c> passa a
    /// ser mais recente que o reconhecimento antigo e o item reaparece.
    /// </summary>
    private async Task<bool> IsAcknowledgedAsync(Guid tenantId, string itemId, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var lastAcknowledgedAt = await _db.AdministrativeAttentionAcknowledgements.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.ItemId == itemId)
            .OrderByDescending(a => a.AcknowledgedAt)
            .Select(a => (DateTimeOffset?)a.AcknowledgedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return lastAcknowledgedAt is not null && lastAcknowledgedAt.Value >= since;
    }

    private sealed record TenantSlice(Guid Id, string Name, TenantStatus Status, DateTimeOffset CreatedAt);

    private sealed record RawAttentionItem(
        string ItemId,
        AttentionItemType Type,
        Guid TenantId,
        string TenantName,
        AttentionSeverity Severity,
        DateTimeOffset Since,
        string Reason);
}
