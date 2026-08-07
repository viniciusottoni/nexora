using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Audit.Support;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.GetAdministrativeTimeline;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — Gherkin "Linha do tempo
/// administrativa". Projeção de LEITURA (RN-004: nenhuma fonte é alterada) sobre oito tabelas já
/// persistidas por outras histórias — criação (<c>tenant.created_at</c>), status (US-153), plano
/// (US-154), proprietário (US-155), credenciais de instalação (US-156), domínio (US-143), suporte
/// (US-145) e incidente de instalação (US-140) — combinadas numa única linha do tempo cronológica.
/// </summary>
internal sealed class GetAdministrativeTimelineQueryHandler
    : IRequestHandler<GetAdministrativeTimelineQuery, Result<AdministrativeTimelineListResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetAdministrativeTimelineQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AdministrativeTimelineListResponse>> Handle(
        GetAdministrativeTimelineQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == request.TenantId && t.DeletedAt == null)
            .Select(t => new { t.Id, t.Name, t.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return Result<AdministrativeTimelineListResponse>.Failure(
                "Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var typeFilter = request.Type.Count == 0 ? null : new HashSet<AdministrativeTimelineEntryType>(request.Type);
        bool Wants(AdministrativeTimelineEntryType type) => typeFilter is null || typeFilter.Contains(type);

        var entries = new List<RawEntry>();

        if (Wants(AdministrativeTimelineEntryType.Creation))
        {
            entries.Add(new RawEntry(
                tenant.Id, AdministrativeTimelineEntryType.Creation, tenant.CreatedAt,
                Actor: null, Origin: "SYSTEM", Reason: "Provisionamento inicial do estabelecimento",
                CorrelationId: null, Summary: $"Estabelecimento \"{tenant.Name}\" criado."));
        }

        if (Wants(AdministrativeTimelineEntryType.StatusChanged))
        {
            var statusRows = await _db.TenantStatusHistories.AsNoTracking()
                .Where(h => h.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);

            entries.AddRange(statusRows.Select(h => new RawEntry(
                h.Id, AdministrativeTimelineEntryType.StatusChanged, h.EffectiveAt,
                h.ActorId, h.Origin, h.Reason, h.DomainEventId?.ToString(),
                $"Status alterado de {StatusLabel(h.PreviousStatus)} para {StatusLabel(h.NewStatus)}.")));
        }

        if (Wants(AdministrativeTimelineEntryType.PlanChanged))
        {
            var planRows = await _db.TenantPlanHistories.AsNoTracking()
                .Where(h => h.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);

            entries.AddRange(planRows.Select(h => new RawEntry(
                h.Id, AdministrativeTimelineEntryType.PlanChanged, h.RequestedAt,
                h.ActorId, "PLATFORM_ADMIN", h.Reason, h.DomainEventId?.ToString(),
                $"Plano alterado de {h.PreviousPlan} para {h.NextPlan}" +
                    (h.AppliedAt is null ? $" (agendado para {h.EffectiveAt:d})." : "."))));
        }

        if (Wants(AdministrativeTimelineEntryType.OwnerChanged))
        {
            var ownerRows = await _db.OwnershipTransfers.AsNoTracking()
                .Where(o => o.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);

            entries.AddRange(ownerRows.Select(o => new RawEntry(
                o.Id, AdministrativeTimelineEntryType.OwnerChanged, o.TransferredAt,
                o.ActorId, "PLATFORM_ADMIN", o.Reason, CorrelationId: null,
                "Titularidade transferida para novo proprietário.")));
        }

        if (Wants(AdministrativeTimelineEntryType.CredentialsReissued))
        {
            var credentialRows = await _db.InstallationCredentials.AsNoTracking()
                .Where(c => c.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);

            entries.AddRange(credentialRows.Select(c => new RawEntry(
                c.Id, AdministrativeTimelineEntryType.CredentialsReissued, c.CreatedAt,
                c.ActorId, "PLATFORM_ADMIN", c.Reason ?? "Emissão de credencial de instalação", CorrelationId: null,
                "Token de instalação emitido/reemitido.")));
        }

        if (Wants(AdministrativeTimelineEntryType.DomainRegistered))
        {
            var domainRows = await _db.TenantDomains.AsNoTracking()
                .Where(d => d.TenantId == tenant.Id && d.DeletedAt == null)
                .ToListAsync(cancellationToken);

            entries.AddRange(domainRows.Select(d => new RawEntry(
                d.Id, AdministrativeTimelineEntryType.DomainRegistered, d.CreatedAt,
                Actor: null, Origin: "PLATFORM_ADMIN", Reason: "Registro de domínio próprio", CorrelationId: null,
                $"Domínio \"{d.Domain}\" registrado.")));
        }

        if (Wants(AdministrativeTimelineEntryType.SupportGranted))
        {
            var supportRows = await _db.SupportAccesses.AsNoTracking()
                .Where(s => s.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);

            entries.AddRange(supportRows.Select(s => new RawEntry(
                s.Id, AdministrativeTimelineEntryType.SupportGranted, s.GrantedAt,
                s.GrantedTo, "PLATFORM_ADMIN", s.Reason, CorrelationId: null,
                $"Acesso de suporte concedido por {s.DurationMinutes} min.")));
        }

        if (Wants(AdministrativeTimelineEntryType.Incident))
        {
            var incidentRows = await _db.InstallationIncidents.AsNoTracking()
                .Where(i => i.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);

            entries.AddRange(incidentRows.Select(i => new RawEntry(
                i.Id, AdministrativeTimelineEntryType.Incident, i.StartedAt,
                Actor: null, Origin: "SYSTEM", Reason: i.Cause ?? "Detecção automática de saúde",
                CorrelationId: null,
                $"Incidente de instalação ({(i.Type == InstallationIncidentType.Offline ? "fora do ar" : "degradada")})" +
                    (i.ResolvedAt is null ? " — em aberto." : $" — resolvido em {i.ResolvedAt:g}."))));
        }

        if (request.From is { } from)
            entries = entries.Where(e => e.OccurredAt >= from).ToList();

        if (request.To is { } to)
            entries = entries.Where(e => e.OccurredAt <= to).ToList();

        if (request.ActorId is { } actorId)
            entries = entries.Where(e => e.Actor == actorId).ToList();

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            entries = entries.Where(e => string.Equals(e.CorrelationId, request.CorrelationId, StringComparison.OrdinalIgnoreCase)).ToList();

        entries = entries.OrderBy(e => e.OccurredAt).ThenBy(e => e.Id).ToList();

        var cursorValue = AuditLogCursor.Decode(request.Cursor);
        var afterCursor = cursorValue is null
            ? entries
            : entries.Where(e => IsAfterCursor(e, cursorValue.Value)).ToList();

        var page = afterCursor.Take(request.Limit + 1).ToList();
        var hasMore = page.Count > request.Limit;
        if (hasMore)
            page = page.Take(request.Limit).ToList();

        var actorIds = page.Where(e => e.Actor is not null).Select(e => e.Actor!.Value).Distinct().ToList();
        var actorNames = actorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.AsNoTracking()
                .Where(u => actorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var data = page.Select(e => new AdministrativeTimelineEntryResponse(
                e.Type.ToWireLabel(),
                e.OccurredAt,
                e.Actor is { } actorId
                    // Ator resolvido só quando pertence ao MESMO tenant já em contexto RLS (proprietário/
                    // usuário local); ações de administrador de plataforma [HIPÓTESE] não resolvem nome
                    // aqui (o app_user do administrador vive fora do escopo deste tenant) — mostrado com
                    // rótulo genérico em vez de tentar uma segunda consulta sem contexto RLS válido.
                    ? new AdministrativeTimelineActorResponse(actorId, actorNames.TryGetValue(actorId, out var name) ? name : "Administrador da plataforma")
                    : null,
                e.Origin,
                e.Reason,
                e.CorrelationId,
                e.Summary))
            .ToList();

        var nextCursor = hasMore && page.Count > 0
            ? AuditLogCursor.Encode(page[^1].OccurredAt, page[^1].Id)
            : null;

        return Result<AdministrativeTimelineListResponse>.Success(new AdministrativeTimelineListResponse(data, nextCursor));
    }

    private static bool IsAfterCursor(RawEntry entry, (DateTimeOffset OccurredAt, Guid Id) cursor)
    {
        if (entry.OccurredAt != cursor.OccurredAt)
            return entry.OccurredAt > cursor.OccurredAt;

        return entry.Id.CompareTo(cursor.Id) > 0;
    }

    private static string StatusLabel(TenantStatus status) => status switch
    {
        TenantStatus.Provisioned => "Provisionado",
        TenantStatus.Installing => "Instalando",
        TenantStatus.Active => "Ativo",
        TenantStatus.Suspended => "Suspenso",
        TenantStatus.Cancelled => "Cancelado",
        _ => status.ToString()
    };

    private sealed record RawEntry(
        Guid Id,
        AdministrativeTimelineEntryType Type,
        DateTimeOffset OccurredAt,
        Guid? Actor,
        string Origin,
        string Reason,
        string? CorrelationId,
        string Summary);
}
