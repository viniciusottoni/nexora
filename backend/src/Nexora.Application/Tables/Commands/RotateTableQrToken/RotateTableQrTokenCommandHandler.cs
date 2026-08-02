using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Operation.Abstractions;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.RotateTableQrToken;

/// <summary>
/// A troca do <see cref="Domain.Operation.DiningTable.QrToken"/> É a invalidação do código
/// anterior: não existe uma lista de "tokens revogados" a consultar — como só o valor atual de
/// <c>qr_token</c> é aceito (constraint <c>UNIQUE</c> + comparação exata na resolução do QR),
/// substituir o valor já basta para o código antigo parar de funcionar (US-020, cenário
/// "Rotação de token": "o código anterior deve deixar de funcionar" — sem prazo de carência).
/// </summary>
internal sealed class RotateTableQrTokenCommandHandler : IRequestHandler<RotateTableQrTokenCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IQrTokenGenerator _qrTokenGenerator;

    public RotateTableQrTokenCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, IQrTokenGenerator qrTokenGenerator)
    {
        _db = db;
        _tenantContext = tenantContext;
        _qrTokenGenerator = qrTokenGenerator;
    }

    public async Task<Result> Handle(RotateTableQrTokenCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var table = await _db.DiningTables.SingleOrDefaultAsync(
            t => t.Id == request.TableId && t.TenantId == tenantId && t.DeletedAt == null, cancellationToken);
        if (table is null)
        {
            return Result.Failure("Mesa não encontrada.", ApiErrorCodes.TableNotFound);
        }

        // Colisão do novo token com um token existente é astronomicamente improvável (entropia de
        // IQrTokenGenerator), mas o retry abaixo não custa nada e blinda contra o pior caso sem
        // exigir tratamento especial de DbUpdateException por violação de UNIQUE no chamador.
        string newToken;
        var attempts = 0;
        do
        {
            newToken = _qrTokenGenerator.Generate();
            attempts++;
        }
        while (attempts < 5 && await _db.DiningTables.AnyAsync(t => t.QrToken == newToken, cancellationToken));

        var now = DateTimeOffset.UtcNow;
        table.RotateQrToken(newToken);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "TABLE_QR_TOKEN_ROTATED",
            entity: "dining_table",
            occurredAt: now,
            storeId: table.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: table.Id,
            reason: "QR Code impresso comprometido/fotografado"));

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "tenant.config_updated",
            aggregateType: "dining_table",
            aggregateId: table.Id,
            payload: JsonSerializer.Serialize(new { areaId = table.AreaId, tableId = table.Id, action = "qr_token_rotated" }),
            origin: "CLOUD",
            occurredAt: now,
            storeId: table.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        return Result.Success();
    }
}
