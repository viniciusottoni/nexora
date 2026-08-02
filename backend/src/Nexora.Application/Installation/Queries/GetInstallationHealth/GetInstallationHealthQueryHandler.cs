using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Installation.Abstractions;
using Nexora.Contracts.Installation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Installation.Queries.GetInstallationHealth;

/// <summary>
/// Porta de <c>SystemInstallationHealthProbe.inspect()</c>. Diferença deliberada em relação ao
/// original: o status de sincronização com a nuvem não faz uma chamada HTTP síncrona a cada
/// requisição de saúde — ele lê o último resultado gravado por <c>PollSyncHealthCommand</c>
/// (disparado periodicamente pelo <c>SyncOutboxWorker</c>) em <c>EdgeInstallation.Health</c>.
/// Isso evita que um endpoint de health check fique refém da latência/indisponibilidade da
/// internet da loja — o próprio ADR-001 (local-first) recomenda não depender de rede aqui.
/// </summary>
internal sealed class GetInstallationHealthQueryHandler
    : IRequestHandler<GetInstallationHealthQuery, Result<InstallationHealthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisHealthChecker _redis;
    private readonly IAppVersionProvider _version;

    public GetInstallationHealthQueryHandler(IApplicationDbContext db, IRedisHealthChecker redis, IAppVersionProvider version)
    {
        _db = db;
        _redis = redis;
        _version = version;
    }

    public async Task<Result<InstallationHealthResponse>> Handle(
        GetInstallationHealthQuery request,
        CancellationToken cancellationToken)
    {
        string postgres;
        int pendingEvents;
        DateTimeOffset? lastSyncAt;
        string sync = "UNKNOWN";

        try
        {
            pendingEvents = await _db.OutboxEntries.AsNoTracking()
                .CountAsync(o => o.Status != "SYNCED", cancellationToken);

            var installation = await _db.EdgeInstallations.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            lastSyncAt = installation?.LastSeenAt;

            if (installation is not null)
            {
                sync = ExtractSyncStatus(installation.Health);
            }

            postgres = "OK";
        }
        catch
        {
            // Health check não deve derrubar o endpoint por uma falha transitória de conexão —
            // reporta DOWN em vez de propagar a exceção (mesmo comportamento do probe original,
            // que envolve cada checagem em try/catch isolado).
            postgres = "DOWN";
            pendingEvents = 0;
            lastSyncAt = null;
        }

        var redis = await SafePingAsync(cancellationToken);

        return Result<InstallationHealthResponse>.Success(new InstallationHealthResponse(
            Postgres: postgres,
            Redis: redis,
            Sync: sync,
            PendingEvents: pendingEvents,
            LastSyncAt: lastSyncAt,
            Version: _version.CurrentVersion));
    }

    private async Task<string> SafePingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var status = await _redis.PingAsync(cancellationToken);
            return status.ToString().ToUpperInvariant();
        }
        catch
        {
            return "DOWN";
        }
    }

    private static string ExtractSyncStatus(string healthJson)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(healthJson);
            if (document.RootElement.TryGetProperty("sync", out var syncProperty) &&
                syncProperty.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return syncProperty.GetString() ?? "UNKNOWN";
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Health ainda no default "{}" ou em formato antigo — trata como desconhecido.
        }

        return "UNKNOWN";
    }
}
