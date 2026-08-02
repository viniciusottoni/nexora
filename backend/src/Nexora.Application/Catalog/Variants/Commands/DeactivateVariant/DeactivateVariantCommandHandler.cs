using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Variants;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Variants.Commands.DeactivateVariant;

internal sealed class DeactivateVariantCommandHandler : IRequestHandler<DeactivateVariantCommand, Result<VariantResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public DeactivateVariantCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<VariantResponse>> Handle(DeactivateVariantCommand request, CancellationToken cancellationToken)
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

        if (variant.IsActive)
        {
            variant.Deactivate();

            _db.AuditLogs.Add(AuditLog.Create(
                tenantId, action: "PRODUCT_VARIANT_DEACTIVATED", entity: "product_variant",
                occurredAt: now, actorId: actorId, deviceId: _tenantContext.DeviceId, entityId: variant.Id));

            // EVT-050 product.updated (US-011 §6) — desativação nunca é SoftDelete/DeletedAt
            // (mesmo espírito de DeactivateProductCommandHandler, US-010): histórico preservado.
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId, type: "product.updated", aggregateType: "product", aggregateId: variant.ProductId,
                payload: JsonSerializer.Serialize(new { productId = variant.ProductId, variantId = variant.Id, changedKeys = new[] { "isActive" } }),
                origin: "CLOUD", occurredAt: now, actorId: actorId, deviceId: _tenantContext.DeviceId));
        }

        var currentPrice = await _db.Prices
            .AsNoTracking()
            .CurrentDineInFor(variant.Id)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<VariantResponse>.Success(new VariantResponse(
            variant.Id, variant.ProductId, variant.Name, variant.Sku, variant.SizeCode,
            variant.PrepMinutes, variant.IsDefault, variant.IsActive,
            currentPrice?.Amount, currentPrice?.Channel.ToString()));
    }
}
