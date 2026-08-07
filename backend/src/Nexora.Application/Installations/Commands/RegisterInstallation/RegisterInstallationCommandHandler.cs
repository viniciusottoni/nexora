using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Abstractions;
using Nexora.Application.Installations.Support;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Installations;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Installations.Commands.RegisterInstallation;

/// <summary>
/// Porta de <c>RegisterInstallationService.execute</c> + a função SQL <c>register_edge_installation</c>
/// original, agora expressa diretamente sobre <see cref="EdgeInstallation"/> (que já carrega os
/// campos de token — não existe uma tabela de token separada nesta portabilidade, ver ADR-039).
/// Idempotente: reenviar a mesma chave pública para uma instalação já concluída não é erro.
/// </summary>
internal sealed class RegisterInstallationCommandHandler
    : IRequestHandler<RegisterInstallationCommand, Result<RegisterInstallationResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IInstallationTokenDigester _tokenDigester;
    private readonly ICloudEndpointOptions _cloudEndpoint;
    private readonly IPinLookupPepperProvider _pinLookupPepper;
    private readonly ILogger<RegisterInstallationCommandHandler> _logger;

    public RegisterInstallationCommandHandler(
        IApplicationDbContext db,
        IInstallationTokenDigester tokenDigester,
        ICloudEndpointOptions cloudEndpoint,
        IPinLookupPepperProvider pinLookupPepper,
        ILogger<RegisterInstallationCommandHandler> logger)
    {
        _db = db;
        _tokenDigester = tokenDigester;
        _cloudEndpoint = cloudEndpoint;
        _pinLookupPepper = pinLookupPepper;
        _logger = logger;
    }

    public async Task<Result<RegisterInstallationResponse>> Handle(
        RegisterInstallationCommand request,
        CancellationToken cancellationToken)
    {
        var locatedInstallation = await PlatformInstallationLookup.FindAsync(
            _db, request.InstallationId, cancellationToken);

        if (locatedInstallation is null)
        {
            return Result<RegisterInstallationResponse>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        var installation = await _db.LockEdgeInstallationForUpdateAsync(
            locatedInstallation.Id, cancellationToken);
        if (installation is null)
        {
            return Result<RegisterInstallationResponse>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        if (installation.IsInstalled)
        {
            if (!SafeEquals(installation.PublicKey!, request.PublicKey))
            {
                return Result<RegisterInstallationResponse>.Failure(
                    "Esta instalação já foi concluída com outra chave pública.",
                    ApiErrorCodes.InstallationPublicKeyPermissionDenied);
            }

            // Repetição do mesmo registro (rede instável, retry do edge) — devolve a mesma
            // resposta sem consumir nada de novo (mesmo comportamento de findCompleted no TS).
            return Result<RegisterInstallationResponse>.Success(
                await BuildResponseAsync(installation, cancellationToken));
        }

        var tokenHash = _tokenDigester.Digest(request.RawToken);
        if (installation.InstallTokenHash is null || !SafeEquals(installation.InstallTokenHash, tokenHash))
        {
            // Token errado/ausente é tratado como "não encontrado" — não revela se a instalação
            // existe (mesma regra de segurança do ADR-021 para recurso de outro tenant).
            return Result<RegisterInstallationResponse>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        if (installation.IsTokenExpired(DateTimeOffset.UtcNow))
        {
            return Result<RegisterInstallationResponse>.Failure(
                "O token de instalação expirou.", ApiErrorCodes.InstallationTokenExpired);
        }

        installation.CompleteRegistration(request.PublicKey, request.Version, request.Hostname);

        // US-153 · Ciclo de vida do estabelecimento — instalação edge registrada é o gatilho de
        // PROVISIONED → INSTALLING (doc §4, cenário "Ativação após implantação" pressupõe um tenant
        // já INSTALLING). Só dispara na primeira instalação de um tenant recém-provisionado; um
        // tenant já Active/Suspended/etc. que reinstala (nova loja, chave rotacionada) não regride
        // de status.
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == installation.TenantId, cancellationToken);
        if (tenant is not null && tenant.Status == TenantStatus.Provisioned)
        {
            var transitionAt = DateTimeOffset.UtcNow;
            var previousStatus = tenant.TransitionStatus(TenantStatus.Installing, transitionAt);

            var statusChangedEvent = DomainEvent.Create(
                tenant.Id,
                type: "tenant.status_changed",
                aggregateType: "tenant",
                aggregateId: tenant.Id,
                payload: JsonSerializer.Serialize(new
                {
                    tenantId = tenant.Id,
                    previousStatus = previousStatus.ToWireLabel(),
                    status = TenantStatus.Installing.ToWireLabel(),
                    reason = "Instalação edge registrada.",
                    effectiveAt = transitionAt,
                    actorId = (Guid?)null,
                }),
                origin: "CLOUD",
                occurredAt: transitionAt,
                storeId: installation.StoreId,
                installationId: installation.Id);
            _db.DomainEvents.Add(statusChangedEvent);

            _db.TenantStatusHistories.Add(TenantStatusHistory.Create(
                tenant.Id, previousStatus, TenantStatus.Installing, "Instalação edge registrada.",
                origin: "SYSTEM", effectiveAt: transitionAt, domainEventId: statusChangedEvent.Id));
        }

        _db.AuditLogs.Add(AuditLog.Create(
            installation.TenantId,
            action: "EDGE_INSTALLATION_REGISTERED",
            entity: nameof(EdgeInstallation),
            occurredAt: DateTimeOffset.UtcNow,
            storeId: installation.StoreId,
            entityId: installation.Id,
            after: $$"""{"version":"{{request.Version}}","hostname":"{{request.Hostname}}"}"""));

        _db.DomainEvents.Add(DomainEvent.Create(
            installation.TenantId,
            type: "edge_installation.registered",
            aggregateType: nameof(EdgeInstallation),
            aggregateId: installation.Id,
            payload: $$"""{"version":"{{request.Version}}"}""",
            origin: "CLOUD",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: installation.StoreId,
            installationId: installation.Id));

        // SaveChangesAsync é feito pelo TransactionBehavior (commands).

        _logger.LogInformation(
            "Instalação edge registrada. InstallationId={InstallationId}, TenantId={TenantId}",
            installation.Id, installation.TenantId);

        return Result<RegisterInstallationResponse>.Success(
            await BuildResponseAsync(installation, cancellationToken));
    }

    private async Task<RegisterInstallationResponse> BuildResponseAsync(
        EdgeInstallation installation, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstAsync(t => t.Id == installation.TenantId, cancellationToken);
        var store = await _db.Stores.AsNoTracking()
            .FirstAsync(s => s.Id == installation.StoreId, cancellationToken);
        var config = await _db.TenantConfigs.AsNoTracking()
            .FirstAsync(c => c.TenantId == installation.TenantId, cancellationToken);

        return new RegisterInstallationResponse(
            new RegisteredTenant(tenant.Id, tenant.Name, tenant.Slug),
            new RegisteredStore(store.Id, store.Name, store.Timezone),
            config.ConfigVersion,
            $"{_cloudEndpoint.PublicUrl.TrimEnd('/')}/v1/sync",
            _pinLookupPepper.Derive(tenant.Id));
    }

    /// <summary>Comparação de tempo constante evitada aqui de propósito: hash já é opaco (SHA-256/Argon2), timing não vaza segredo útil — mesma simplificação do original (<c>safePublicKeyMatch</c>).</summary>
    private static bool SafeEquals(string left, string right) => left.Length == right.Length && left == right;
}
