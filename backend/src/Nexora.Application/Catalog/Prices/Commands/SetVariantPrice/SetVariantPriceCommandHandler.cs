using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Variants;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Prices.Commands.SetVariantPrice;

/// <summary>
/// Cenário Gherkin "Alteração de preço registrada" (US-011 §4): fecha o preço vigente do canal
/// (<see cref="Price.Close"/>, <c>ValidTo = now</c>) e cria uma nova linha (<see cref="Price.Create"/>,
/// <c>ValidFrom = now</c>) — nunca edita a linha antiga. Sem preço vigente prévio no canal, apenas
/// cria a primeira linha (nada para fechar). Chamada com o mesmo valor do preço vigente é um no-op
/// deliberado (não gera linha nem evento redundante — mesmo espírito condicional de
/// <c>UpdateStationCommandHandler</c>/<c>UpdateRoleCommandHandler</c>, US-017/plataforma: só
/// registra evento quando algo de fato mudou).
/// </summary>
internal sealed class SetVariantPriceCommandHandler : IRequestHandler<SetVariantPriceCommand, Result<PriceResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public SetVariantPriceCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PriceResponse>> Handle(SetVariantPriceCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<PriceResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!ChannelParser.TryParse(request.Channel, out var channel))
        {
            return Result<PriceResponse>.Failure("Canal de venda inválido.", ApiErrorCodes.PriceChannelInvalid);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.TenantId == tenantId && v.DeletedAt == null, cancellationToken);

        if (variant is null)
        {
            return Result<PriceResponse>.Failure("Variante não encontrada.", ApiErrorCodes.VariantNotFound);
        }

        var currentPrice = await _db.Prices
            .AsNoTracking()
            .Where(p => p.VariantId == variant.Id && p.Channel == channel && p.ValidTo == null)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentPrice is not null && currentPrice.Amount == request.Amount)
        {
            // Mesmo valor do preço vigente — no-op, não historiza nem emite evento.
            return Result<PriceResponse>.Success(new PriceResponse(
                currentPrice.Id, currentPrice.VariantId, currentPrice.Channel.ToString(), currentPrice.Amount, currentPrice.ValidFrom, currentPrice.ValidTo));
        }

        var oldAmount = currentPrice?.Amount;
        if (currentPrice is not null)
        {
            await _db.Prices
                .Where(p => p.Id == currentPrice.Id && p.ValidTo == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.ValidTo, now), cancellationToken);
        }

        var newPrice = Price.Create(tenantId, variant.Id, channel, request.Amount, actorId, now);
        _db.Prices.Add(newPrice);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "VARIANT_PRICE_CHANGED",
            entity: "price",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            entityId: newPrice.Id,
            before: oldAmount is null ? null : JsonSerializer.Serialize(new { amount = oldAmount, channel = channel.ToString() }),
            after: JsonSerializer.Serialize(new { amount = newPrice.Amount, channel = channel.ToString() })));

        // EVT-052 price.changed (US-011 §6) representa alteração de um preço já vigente. A
        // primeira definição não possui oldAmount e, portanto, não é uma mudança historizada.
        if (oldAmount is not null)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "price.changed",
                aggregateType: "product_variant",
                aggregateId: variant.Id,
                payload: JsonSerializer.Serialize(new
                {
                    variantId = variant.Id,
                    oldAmount,
                    newAmount = newPrice.Amount,
                    validFrom = newPrice.ValidFrom
                }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<PriceResponse>.Success(new PriceResponse(
            newPrice.Id, newPrice.VariantId, newPrice.Channel.ToString(), newPrice.Amount, newPrice.ValidFrom, newPrice.ValidTo));
    }
}
