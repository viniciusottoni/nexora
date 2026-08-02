using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Commands.ReorderProducts;

internal sealed class ReorderProductsCommandHandler : IRequestHandler<ReorderProductsCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ReorderProductsCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(ReorderProductsCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var categoryExists = await _db.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CategoryId && c.TenantId == tenantId && c.DeletedAt == null, cancellationToken);

        if (!categoryExists)
        {
            return Result.Failure("Categoria não encontrada.", ApiErrorCodes.CategoryNotFound);
        }

        var products = await _db.Products
            .Where(p => p.TenantId == tenantId && p.CategoryId == request.CategoryId && p.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var existingIds = products.Select(p => p.Id).ToHashSet();
        var requestedIds = request.Order.ToHashSet();

        if (!existingIds.SetEquals(requestedIds))
        {
            return Result.Failure(
                "A lista enviada não corresponde aos produtos existentes nesta categoria.",
                ApiErrorCodes.CatalogReorderSetMismatch);
        }

        var byId = products.ToDictionary(p => p.Id);
        var changed = false;

        for (var index = 0; index < request.Order.Count; index++)
        {
            var product = byId[request.Order[index]];
            var newPosition = (short)index;
            if (product.SortOrder == newPosition)
            {
                continue;
            }

            product.UpdateSortOrder(newPosition);
            changed = true;
        }

        if (changed)
        {
            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "PRODUCT_REORDERED",
                entity: "product",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: request.CategoryId,
                after: JsonSerializer.Serialize(new { categoryId = request.CategoryId, order = request.Order })));

            // EVT-050 product.updated — um único evento para o lote reordenado, ancorado na
            // categoria (não em cada produto individualmente, para não inundar o outbox de sync em
            // reordenações grandes — US-010 §12, "Cardápio com 200 produtos").
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "product.updated",
                aggregateType: "category",
                aggregateId: request.CategoryId,
                payload: JsonSerializer.Serialize(new { categoryId = request.CategoryId, order = request.Order, changedKeys = new[] { "position" } }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result.Success();
    }
}
