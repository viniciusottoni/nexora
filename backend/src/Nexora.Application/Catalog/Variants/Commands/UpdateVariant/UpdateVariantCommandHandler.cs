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

namespace Nexora.Application.Catalog.Variants.Commands.UpdateVariant;

internal sealed class UpdateVariantCommandHandler : IRequestHandler<UpdateVariantCommand, Result<VariantResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateVariantCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<VariantResponse>> Handle(UpdateVariantCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<VariantResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.TenantId == tenantId && v.DeletedAt == null, cancellationToken);

        if (variant is null)
        {
            return Result<VariantResponse>.Failure("Variante não encontrada.", ApiErrorCodes.VariantNotFound);
        }

        var before = JsonSerializer.Serialize(new { name = variant.Name, sizeCode = variant.SizeCode, sku = variant.Sku });

        var changedKeys = new List<string>();
        if (request.Name != variant.Name) changedKeys.Add("name");
        if (request.SizeCode != variant.SizeCode) changedKeys.Add("sizeCode");
        if (request.Sku != variant.Sku) changedKeys.Add("sku");

        variant.UpdateDetails(request.Name, request.Sku, request.SizeCode);

        if (changedKeys.Count > 0)
        {
            var after = JsonSerializer.Serialize(new { name = variant.Name, sizeCode = variant.SizeCode, sku = variant.Sku });

            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "PRODUCT_VARIANT_UPDATED",
                entity: "product_variant",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: variant.Id,
                before: before,
                after: after));

            // EVT-050 product.updated (US-011 §6).
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "product.updated",
                aggregateType: "product",
                aggregateId: variant.ProductId,
                payload: JsonSerializer.Serialize(new { productId = variant.ProductId, variantId = variant.Id, changedKeys }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        var currentPrice = await _db.Prices
            .AsNoTracking()
            .CurrentDineInFor(variant.Id)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<VariantResponse>.Success(new VariantResponse(
            variant.Id,
            variant.ProductId,
            variant.Name,
            variant.Sku,
            variant.SizeCode,
            variant.PrepMinutes,
            variant.IsDefault,
            variant.IsActive,
            currentPrice?.Amount,
            currentPrice?.Channel.ToString()));
    }
}
