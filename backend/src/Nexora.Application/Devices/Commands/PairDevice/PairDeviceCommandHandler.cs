using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Devices.Abstractions;
using Nexora.Application.Devices; // DeviceKindMapper, DeviceSnapshot
using Nexora.Contracts.Devices;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Devices.Commands.PairDevice;

internal sealed class PairDeviceCommandHandler : IRequestHandler<PairDeviceCommand, Result<PairDeviceResponse>>
{
    // Porta de LocalPairingRateLimiter (apps/api-edge/src/modules/devices/pairing-rate-limiter.ts):
    // 5 tentativas por janela de 15 min. O TS original guarda isso em memória do processo
    // (Map<string, AttemptWindow>), o que não sobrevive a restart nem funciona com múltiplas
    // instâncias do edge. Aqui persistimos a contagem em PairingCode.Attempts — campo que já
    // existia no schema/domínio mas nunca era incrementado no TS original — contando tentativas
    // rejeitadas contra o código de pareamento ativo da loja dentro da janela.
    // TODO: mover para Redis se a volumetria de pareamento por loja justificar.
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);
    private const short RateLimitMaxAttempts = 5;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IDeviceSecretGenerator _deviceSecretGenerator;
    private readonly ISecretDigester _secretDigester;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ILogger<PairDeviceCommandHandler> _logger;

    public PairDeviceCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IDeviceSecretGenerator deviceSecretGenerator,
        ISecretDigester secretDigester,
        IEventOriginProvider eventOrigin,
        ILogger<PairDeviceCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _deviceSecretGenerator = deviceSecretGenerator;
        _secretDigester = secretDigester;
        _eventOrigin = eventOrigin;
        _logger = logger;
    }

    public async Task<Result<PairDeviceResponse>> Handle(PairDeviceCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<PairDeviceResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (_tenantContext.StoreId is null)
        {
            return Result<PairDeviceResponse>.Failure(
                "Loja não definida para esta requisição.",
                ApiErrorCodes.DeviceStoreContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var storeId = _tenantContext.StoreId.Value;
        var now = DateTimeOffset.UtcNow;
        var windowStart = now - RateLimitWindow;

        var activeCode = await _db.PairingCodes
            .Where(p => p.TenantId == tenantId && p.StoreId == storeId && p.ConsumedAt == null && p.CreatedAt >= windowStart)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCode is not null && activeCode.Attempts >= RateLimitMaxAttempts)
        {
            var retryAfterSeconds = (int)Math.Max(1, Math.Ceiling((activeCode.CreatedAt + RateLimitWindow - now).TotalSeconds));
            return Result<PairDeviceResponse>.Failure(
                $"Muitas tentativas de pareamento. Tente novamente em {retryAfterSeconds} segundos.",
                ApiErrorCodes.DevicePairingRateLimited);
        }

        var codeHash = _secretDigester.Digest(request.Code);

        var pairingCode = await _db.PairingCodes
            .Where(p => p.TenantId == tenantId && p.StoreId == storeId && p.CodeHash == codeHash)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pairingCode is null)
        {
            // Código não corresponde a nenhum código emitido — registra a tentativa contra o
            // código ativo da loja (se houver) para acionar o rate limit em brute-force.
            activeCode?.RecordAttempt();
            await SaveAttemptAsync(cancellationToken);
            return Result<PairDeviceResponse>.Failure(
                "Código de pareamento inválido.",
                ApiErrorCodes.DevicePairingCodeInvalid);
        }

        if (pairingCode.IsConsumed)
        {
            pairingCode.RecordAttempt();
            await SaveAttemptAsync(cancellationToken);
            return Result<PairDeviceResponse>.Failure(
                "Código de pareamento já utilizado.",
                ApiErrorCodes.DevicePairingCodeConsumed);
        }

        if (pairingCode.IsExpired(now))
        {
            pairingCode.RecordAttempt();
            await SaveAttemptAsync(cancellationToken);
            return Result<PairDeviceResponse>.Failure(
                "Código de pareamento expirado.",
                ApiErrorCodes.DevicePairingCodeExpired);
        }

        pairingCode.Consume();

        var deviceSecret = _deviceSecretGenerator.Generate();
        var secretHash = _secretDigester.Digest(deviceSecret);
        var deviceType = DeviceKindMapper.ToDeviceType(request.Kind);

        var device = Device.Create(tenantId, storeId, request.Label.Trim(), deviceType, request.Fingerprint.Trim());
        device.SetSecret(secretHash);

        _db.Devices.Add(device);

        // O ator do registro é quem gerou o código (pairingCode.CreatedBy), não o dispositivo
        // anônimo que está se pareando — mesma escolha do TS original.
        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "DEVICE_REGISTERED",
            entity: "device",
            occurredAt: now,
            storeId: storeId,
            actorId: pairingCode.CreatedBy,
            deviceId: device.Id,
            entityId: device.Id,
            after: DeviceSnapshot.ToJson(device)));

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId: tenantId,
            type: "device.registered",
            aggregateType: "Device",
            aggregateId: device.Id,
            payload: JsonSerializer.Serialize(new
            {
                deviceId = device.Id,
                label = device.Label,
                kind = request.Kind,
                registeredBy = pairingCode.CreatedBy,
            }),
            origin: _eventOrigin.Origin,
            occurredAt: now,
            storeId: storeId,
            actorId: pairingCode.CreatedBy,
            deviceId: device.Id));

        // SaveChangesAsync é feito pelo TransactionBehavior (commands) — estado e evento na
        // mesma transação (ADR-006).

        _logger.LogInformation(
            "Dispositivo pareado. TenantId={TenantId}, StoreId={StoreId}, DeviceId={DeviceId}, Kind={Kind}",
            tenantId, storeId, device.Id, request.Kind);

        return Result<PairDeviceResponse>.Success(
            new PairDeviceResponse(new PairedDeviceInfo(device.Id, device.Label), deviceSecret));
    }

    /// <summary>
    /// Persiste o contador de tentativas IMEDIATAMENTE, fora do caminho normal de
    /// <c>SaveChangesAsync</c> do <c>TransactionBehavior</c> — que só salva quando o handler
    /// devolve <c>Result.Success</c> ("Se o handler já retornou falha, não salva nada", ver
    /// docstring da classe). Esses três caminhos de falha (código inválido/consumido/expirado)
    /// são a ÚNICA razão de existir de <see cref="PairingCode.Attempts"/> — a US-005 pede
    /// explicitamente "rate limit + expiração curta" contra força bruta (§12, nível Segurança), e
    /// cada tentativa chega numa instância de <c>DbContext</c> nova (uma por requisição HTTP); sem
    /// este save explícito, o contador nunca sairia da memória do processo que atendeu aquela
    /// requisição, o rate limit nunca acionaria entre requisições diferentes, e a US ficaria tão
    /// vulnerável a força bruta quanto se <see cref="PairingCode.Attempts"/> não existisse.
    /// </summary>
    private async Task SaveAttemptAsync(CancellationToken cancellationToken) =>
        await _db.SaveChangesAsync(cancellationToken);
}
