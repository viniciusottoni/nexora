using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Commands.DeactivateProduct;

internal sealed class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand, Result<ProductResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public DeactivateProductCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProductResponse>> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<ProductResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId && p.DeletedAt == null, cancellationToken);

        if (product is null)
        {
            return Result<ProductResponse>.Failure("Produto não encontrado.", ApiErrorCodes.ProductNotFound);
        }

        if (product.IsActive)
        {
            product.Deactivate();

            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "PRODUCT_DEACTIVATED",
                entity: "product",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: product.Id));

            // EVT-050 product.updated (US-010 §6) — "os pedidos históricos devem continuar
            // exibindo o produto corretamente" (§4): desativação nunca é SoftDelete/DeletedAt.
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "product.updated",
                aggregateType: "product",
                aggregateId: product.Id,
                payload: JsonSerializer.Serialize(new { productId = product.Id, changedKeys = new[] { "isActive" } }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        var category = await _db.Categories.AsNoTracking().FirstAsync(c => c.Id == product.CategoryId, cancellationToken);
        var stationName = product.StationId is { } sid
            ? await _db.Stations.AsNoTracking().Where(s => s.Id == sid).Select(s => s.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var imageUrl = await _db.MediaAssets
            .AsNoTracking()
            .Where(m => m.OwnerType == "PRODUCT" && m.OwnerId == product.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => m.Url)
            .FirstOrDefaultAsync(cancellationToken);

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<ProductResponse>.Success(new ProductResponse(
            product.Id,
            product.CategoryId,
            category.Name,
            product.StationId,
            stationName,
            product.Name,
            product.Description,
            product.IngredientsText,
            product.Allergens,
            imageUrl,
            product.SortOrder,
            product.IsActive,
            product.IsAvailable,
            product.AllowsFractions,
            product.MaxFractions));
    }
}
