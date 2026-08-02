using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Commands.UpdateProduct;

internal sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateProductCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProductResponse>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
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

        var categoryId = product.CategoryId;
        if (request.CategoryId is { } requestedCategoryId && requestedCategoryId != product.CategoryId)
        {
            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == requestedCategoryId && c.TenantId == tenantId && c.DeletedAt == null, cancellationToken);

            if (category is null)
            {
                return Result<ProductResponse>.Failure("Categoria não encontrada.", ApiErrorCodes.ProductCategoryNotFound);
            }

            categoryId = category.Id;
        }

        // Guid.Empty é o sentinela reservado para "desvincular a praça" (ver docstring de
        // UpdateProductCommand) — o restante do código-base não tem convenção de "limpar campo
        // opcional via PATCH" (nulo sempre significa "não alterar").
        Guid? stationId = product.StationId;
        if (request.StationId == Guid.Empty)
        {
            stationId = null;
        }
        else if (request.StationId is { } requestedStationId && requestedStationId != product.StationId)
        {
            var station = await _db.Stations
                .FirstOrDefaultAsync(s => s.Id == requestedStationId && s.TenantId == tenantId && s.DeletedAt == null, cancellationToken);

            if (station is null)
            {
                return Result<ProductResponse>.Failure("Praça não encontrada.", ApiErrorCodes.ProductStationNotFound);
            }

            stationId = station.Id;
        }

        var before = JsonSerializer.Serialize(new
        {
            name = product.Name,
            categoryId = product.CategoryId,
            stationId = product.StationId,
            position = product.SortOrder
        });

        var changedKeys = new List<string>();
        if (request.Name is not null && request.Name != product.Name) changedKeys.Add("name");
        if (categoryId != product.CategoryId) changedKeys.Add("categoryId");
        if (stationId != product.StationId) changedKeys.Add("stationId");
        if (request.Description is not null && request.Description != product.Description) changedKeys.Add("description");
        if (request.IngredientsText is not null && request.IngredientsText != product.IngredientsText) changedKeys.Add("ingredientsText");
        if (request.Allergens is not null) changedKeys.Add("allergens");
        if (request.AllowsFractions is not null && request.AllowsFractions != product.AllowsFractions) changedKeys.Add("allowsFractions");
        if (request.MaxFractions is not null && request.MaxFractions != product.MaxFractions) changedKeys.Add("maxFractions");
        if (request.Position is not null && request.Position != product.SortOrder) changedKeys.Add("position");

        product.UpdateDetails(
            request.Name ?? product.Name,
            categoryId,
            stationId,
            request.Description ?? product.Description,
            request.IngredientsText ?? product.IngredientsText,
            request.Allergens ?? product.Allergens,
            request.AllowsFractions ?? product.AllowsFractions,
            request.MaxFractions ?? product.MaxFractions,
            request.Position ?? product.SortOrder);

        if (changedKeys.Count > 0)
        {
            var after = JsonSerializer.Serialize(new
            {
                name = product.Name,
                categoryId = product.CategoryId,
                stationId = product.StationId,
                position = product.SortOrder
            });

            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "PRODUCT_UPDATED",
                entity: "product",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: product.Id,
                before: before,
                after: after));

            // EVT-050 product.updated (US-010 §6).
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "product.updated",
                aggregateType: "product",
                aggregateId: product.Id,
                payload: JsonSerializer.Serialize(new { productId = product.Id, changedKeys }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        var category2 = await _db.Categories.AsNoTracking().FirstAsync(c => c.Id == product.CategoryId, cancellationToken);
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
            category2.Name,
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
