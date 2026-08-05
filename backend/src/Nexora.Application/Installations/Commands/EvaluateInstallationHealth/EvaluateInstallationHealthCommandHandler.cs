using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Support;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Installations.Commands.EvaluateInstallationHealth;

/// <summary>
/// US-140 §4/§9: classifica a saúde da instalação a partir da defasagem de contato observada pela
/// NUVEM (<see cref="InstallationHealthClassifier"/>, nunca dos eventos de conectividade que o
/// próprio edge emite sobre si mesmo) e reage à transição —
/// abre/fecha <see cref="InstallationIncident"/>, grava o <c>DomainEvent</c> correspondente
/// (mesma transação, ADR-006) e notifica a plataforma quando a instalação vira DOWN
/// (US-140 §4, cenário "Instalação fora do ar").
/// </summary>
internal sealed class EvaluateInstallationHealthCommandHandler
    : IRequestHandler<EvaluateInstallationHealthCommand, Result<EvaluateInstallationHealthResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IPlatformAlertNotifier _platformAlertNotifier;

    public EvaluateInstallationHealthCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IPlatformAlertNotifier platformAlertNotifier)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _platformAlertNotifier = platformAlertNotifier;
    }

    public async Task<Result<EvaluateInstallationHealthResult>> Handle(
        EvaluateInstallationHealthCommand request, CancellationToken cancellationToken)
    {
        // RLS (ADR-004): comando roda fora de requisição HTTP (worker, um tenant por vez) — mesmo
        // mecanismo de EvaluateCloudAlertConditionsCommandHandler/RestoreProductsPastBusinessDayCommand.
        await _db.SetTenantContextAsync(request.TenantId, cancellationToken);

        var installation = await _db.EdgeInstallations
            .FirstOrDefaultAsync(i => i.Id == request.InstallationId && i.TenantId == request.TenantId, cancellationToken);

        if (installation is null)
        {
            return Result<EvaluateInstallationHealthResult>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        var now = DateTimeOffset.UtcNow;
        var status = InstallationHealthClassifier.Classify(now, installation.LastSeenAt);

        var openIncident = await _db.InstallationIncidents
            .FirstOrDefaultAsync(i => i.InstallationId == installation.Id && i.ResolvedAt == null, cancellationToken);

        var transitioned = await ReconcileIncidentAsync(installation, status, openIncident, now, cancellationToken);

        return Result<EvaluateInstallationHealthResult>.Success(
            new EvaluateInstallationHealthResult(status.ToWireLabel(), transitioned));
    }

    private async Task<bool> ReconcileIncidentAsync(
        EdgeInstallation installation,
        InstallationHealthStatus status,
        InstallationIncident? openIncident,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        switch (status)
        {
            case InstallationHealthStatus.Ok:
                if (openIncident is null)
                    return false;

                openIncident.Resolve(now);
                EmitEvent(installation, "installation.health_recovered", now);
                return true;

            case InstallationHealthStatus.Degraded:
                if (openIncident is { Type: InstallationIncidentType.Degraded })
                    return false; // já degradada — nenhuma transição nova.

                if (openIncident is { Type: InstallationIncidentType.Offline })
                    openIncident.Resolve(now); // recuperação parcial: DOWN -> DEGRADED.

                _db.InstallationIncidents.Add(InstallationIncident.Open(
                    installation.TenantId, installation.Id, InstallationIncidentType.Degraded,
                    Cause(installation, now), now));
                EmitEvent(installation, "installation.health_degraded", now);
                return true;

            case InstallationHealthStatus.Down:
                if (openIncident is { Type: InstallationIncidentType.Offline })
                    return false; // já fora do ar — nenhuma transição nova.

                if (openIncident is { Type: InstallationIncidentType.Degraded })
                    openIncident.Resolve(now); // agrava: DEGRADED -> DOWN.

                _db.InstallationIncidents.Add(InstallationIncident.Open(
                    installation.TenantId, installation.Id, InstallationIncidentType.Offline,
                    Cause(installation, now), now));
                EmitEvent(installation, "installation.health_down", now);

                // US-140 §4, cenário "Instalação fora do ar": "a plataforma deve ser alertada automaticamente".
                await _platformAlertNotifier.NotifyInstallationDownAsync(
                    installation.TenantId, installation.Id, installation.Label, installation.LastSeenAt, cancellationToken);
                return true;

            default:
                return false;
        }
    }

    private static string Cause(EdgeInstallation installation, DateTimeOffset now)
    {
        if (installation.LastSeenAt is null)
            return "Instalação nunca sincronizou com a nuvem.";

        var minutesSince = (int)Math.Round((now - installation.LastSeenAt.Value).TotalMinutes);
        return $"Sem contato com a nuvem há {minutesSince} minutos.";
    }

    /// <summary>
    /// Novos tipos de evento (não estavam no catálogo do documento 04 antes desta história —
    /// documentar em <c>docs/04-Eventos-e-Metricas.md</c>): <c>installation.health_degraded</c>,
    /// <c>installation.health_down</c>, <c>installation.health_recovered</c>. Origem sempre "CLOUD"
    /// (US-140 §9 — a classificação é sempre da nuvem, mesmo quando a causa é o edge estar mudo).
    /// </summary>
    private void EmitEvent(EdgeInstallation installation, string type, DateTimeOffset occurredAt)
    {
        _db.DomainEvents.Add(DomainEvent.Create(
            installation.TenantId,
            type: type,
            aggregateType: "edge_installation",
            aggregateId: installation.Id,
            payload: JsonSerializer.Serialize(new { lastSeenAt = installation.LastSeenAt }),
            origin: _eventOrigin.Origin,
            occurredAt: occurredAt,
            storeId: installation.StoreId));
    }
}
