using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Support;
using Nexora.Contracts.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Installations.Queries.Platform.GetInstallationDiagnostics;

/// <summary>
/// US-140 §4 "Diagnóstico remoto" / RN-015 "nenhum dado de negócio do cliente deve ser exposto".
/// <see cref="Contracts.Platform.InstallationHealthCheckResponse"/> vem só do JSONB livre de
/// <c>EdgeInstallation.Health</c> (mesmo payload que <c>PollSyncHealthCommandHandler</c> grava —
/// hoje só <c>sync</c>/<c>httpStatus</c>/<c>checkedAt</c>, por isso <c>Redis</c> sempre volta nulo:
/// não é omissão, é honestidade sobre o que a instalação já reporta hoje).
/// <see cref="Contracts.Platform.InstallationLogEntryResponse"/> é sintetizado a partir de
/// <c>DomainEvent</c>s do PRÓPRIO agregado <c>edge_installation</c> (nunca de pedido/pagamento/
/// cliente) — tailing de log remoto de verdade (arquivo de log do container edge) é um follow-up
/// de infraestrutura, não desta história.
/// </summary>
internal sealed class GetInstallationDiagnosticsQueryHandler
    : IRequestHandler<GetInstallationDiagnosticsQuery, Result<InstallationDiagnosticsResponse>>
{
    private const int MaxRecentLogs = 20;

    private readonly IApplicationDbContext _db;

    public GetInstallationDiagnosticsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<InstallationDiagnosticsResponse>> Handle(
        GetInstallationDiagnosticsQuery request, CancellationToken cancellationToken)
    {
        var installation = await PlatformInstallationLookup.FindAsync(_db, request.InstallationId, cancellationToken);
        if (installation is null)
        {
            return Result<InstallationDiagnosticsResponse>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        // PlatformInstallationLookup já deixa app.tenant_id fixado no tenant dono (última iteração
        // que encontrou a instalação) — reafirma explicitamente para não depender dessa ordem interna.
        await _db.SetTenantContextAsync(installation.TenantId, cancellationToken);

        var healthCheck = ParseHealthCheck(installation.Health);

        var recentEvents = await _db.DomainEvents.AsNoTracking()
            .Where(e => e.AggregateType == "edge_installation" && e.AggregateId == installation.Id)
            .OrderByDescending(e => e.OccurredAt)
            .Take(MaxRecentLogs)
            .Select(e => new { e.OccurredAt, e.Type })
            .ToListAsync(cancellationToken);

        var recentLogs = recentEvents
            .Select(e => new InstallationLogEntryResponse(e.OccurredAt, LevelFor(e.Type), MessageFor(e.Type)))
            .ToList();

        return Result<InstallationDiagnosticsResponse>.Success(new InstallationDiagnosticsResponse(
            HealthCheck: healthCheck,
            RecentLogs: recentLogs,
            // Nenhuma tabela hoje carrega uso de disco/data do último backup do edge (grep por
            // "Disk"/"Backup" em Nexora.Domain não encontrou entidade correspondente) — retorna nulo
            // em vez de inventar; telemetria de infraestrutura do host é um gap a preencher em
            // docs/domain (ver relatório desta história).
            DiskUsagePercent: null,
            LastBackupAt: null));
    }

    private static InstallationHealthCheckResponse ParseHealthCheck(string healthJson)
    {
        try
        {
            using var document = JsonDocument.Parse(healthJson);
            var root = document.RootElement;

            string? postgres = ReadString(root, "postgres");
            string? sync = ReadString(root, "sync");
            DateTimeOffset? checkedAt = null;

            if (root.TryGetProperty("checkedAt", out var checkedAtProperty) &&
                checkedAtProperty.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(checkedAtProperty.GetString(), out var parsedCheckedAt))
            {
                checkedAt = parsedCheckedAt;
            }

            // "redis" nunca é gravado por PollSyncHealthCommandHandler hoje — omite (nulo) em vez
            // de inventar um status que a instalação nunca reportou.
            return new InstallationHealthCheckResponse(postgres, Redis: null, sync, checkedAt);
        }
        catch (JsonException)
        {
            return new InstallationHealthCheckResponse(null, null, null, null);
        }
    }

    private static string? ReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string LevelFor(string eventType) => eventType switch
    {
        "edge.offline_detected" or "installation.health_down" => "ERROR",
        "installation.health_degraded" => "WARN",
        _ => "INFO"
    };

    private static string MessageFor(string eventType) => eventType switch
    {
        "edge.offline_detected" => "Instalação detectou queda da própria conectividade com a nuvem.",
        "edge.reconnected" => "Instalação reconectou com a nuvem.",
        "installation.health_degraded" => "Nuvem classificou a instalação como degradada (sincronização atrasada).",
        "installation.health_down" => "Nuvem classificou a instalação como fora do ar.",
        "installation.health_recovered" => "Instalação voltou a ficar saudável.",
        _ => eventType
    };
}
