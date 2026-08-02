using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Devices.Abstractions;
using Nexora.Contracts.Devices;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Devices.Commands.CreatePairingCode;

internal sealed class CreatePairingCodeCommandHandler
    : IRequestHandler<CreatePairingCodeCommand, Result<CreatePairingCodeResponse>>
{
    // PAIRING_CODE_TTL_MS em device-registry.ts.
    private static readonly TimeSpan PairingCodeTtl = TimeSpan.FromMinutes(10);
    private const int PairingCodeTtlSeconds = 600;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IPairingCodeGenerator _pairingCodeGenerator;
    private readonly ISecretDigester _secretDigester;
    private readonly ILogger<CreatePairingCodeCommandHandler> _logger;

    public CreatePairingCodeCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IPairingCodeGenerator pairingCodeGenerator,
        ISecretDigester secretDigester,
        ILogger<CreatePairingCodeCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _pairingCodeGenerator = pairingCodeGenerator;
        _secretDigester = secretDigester;
        _logger = logger;
    }

    public async Task<Result<CreatePairingCodeResponse>> Handle(
        CreatePairingCodeCommand request,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<CreatePairingCodeResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (_tenantContext.StoreId is null)
        {
            return Result<CreatePairingCodeResponse>.Failure(
                "Loja não definida para esta requisição.",
                ApiErrorCodes.DeviceStoreContextMissing);
        }

        if (_tenantContext.UserId is null)
        {
            return Result<CreatePairingCodeResponse>.Failure(
                "Esta ação exige um gestor autenticado.",
                ApiErrorCodes.DeviceActorRequired);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var storeId = _tenantContext.StoreId.Value;
        var actorId = _tenantContext.UserId.Value;

        var now = DateTimeOffset.UtcNow;
        var code = _pairingCodeGenerator.Generate();
        var expiresAt = now.Add(PairingCodeTtl);
        var codeHash = _secretDigester.Digest(code);

        var pairingCode = PairingCode.Create(tenantId, storeId, codeHash, actorId, expiresAt);
        _db.PairingCodes.Add(pairingCode);

        // SaveChangesAsync é feito pelo TransactionBehavior (commands).

        _logger.LogInformation(
            "Código de pareamento gerado. TenantId={TenantId}, StoreId={StoreId}, ActorId={ActorId}",
            tenantId, storeId, actorId);

        return Result<CreatePairingCodeResponse>.Success(
            new CreatePairingCodeResponse(code, expiresAt, PairingCodeTtlSeconds));
    }
}
